namespace Lumper.Lib.Bsp.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.Lumps;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Lumps.GameLumps;
using Newtonsoft.Json;
using NLog;

public sealed class GameLumpReader(GameLump gamelump, Stream input, long length, IoHandler? handler = null)
    : LumpReader(input)
{
    [JsonIgnore]
    private readonly GameLump _gameLump = gamelump;

    protected override IoHandler? Handler { get; set; } = handler;

    // Using a list instead of dict because some maps (e.g. Valve TF2 maps) can have multiple entries
    // with GameLumpType.Unknown.
    [JsonProperty]
    public List<(GameLumpType, LumpHeaderInfo)> Headers =>
        Lumps.Select(x => (x.Item1 is Lump<GameLumpType> lump ? lump.Type : GameLumpType.Unknown, x.Item2)).ToList();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public GameLumpReader(GameLump gamelump, BinaryReader reader, long length, IoHandler? handler = null)
        : this(gamelump, reader.BaseStream, length, handler) { }

    public void Load()
    {
        Lumps.Clear();
        _gameLump.Lumps.Clear();
        ReadHeader();
        LoadAll();
    }

    protected override void ReadHeader()
    {
        long startPos = BaseStream.Position;
        int count = ReadInt32();
        LumpHeaderInfo? prevHeader = null;
        bool prevCompressed = false;
        for (int i = 0; i < count; i++)
        {
            var type = (GameLumpType)ReadInt32();

            Lump<GameLumpType> lump = type switch
            {
                GameLumpType.sprp => new Sprp(_gameLump.Parent),
                _ => new UnmanagedLump<GameLumpType>(_gameLump.Parent),
            };

            lump.Type = type;
            lump.Flags = ReadUInt16();
            lump.Version = ReadUInt16();

            var header = new LumpHeaderInfo { Offset = ReadInt32(), UncompressedLength = ReadInt32() };

            if (prevHeader != null)
            {
                long actualLength = header.Offset - prevHeader.Offset;
                if (actualLength < 0)
                    actualLength = length - (prevHeader.Offset + prevHeader.Length - startPos);

                prevHeader.CompressedLength = prevCompressed ? actualLength : -1;
            }

            Lumps.Add(new Tuple<Lump, LumpHeaderInfo>(lump, header));

            if (!_gameLump.Lumps.TryAdd(type, lump))
                Logger.Warn($"Key {type} already in gamelumps, skipping");

            Logger.Debug(
                $"Read gamelump {_gameLump.Lumps.Count}  "
                    + $"id: {type} {(int)type}".PadRight(48)
                    + $"flags: {lump.Flags}".PadRight(24)
                    + $"offset: {header.Offset}".PadRight(24)
                    + $"length: {header.UncompressedLength}".PadRight(24)
            );

            prevHeader = header;
            // lump is compressed if the last bit is 1
            prevCompressed = (lump.Flags & 1) == 1;
        }

        // won't set compressedLength on the last entry
        // but it should be 0 and if its not we don't know the length
        // so its probably not compressed
    }
}
