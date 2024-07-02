namespace Lumper.Lib.BSP.IO;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bsp.Enum;
using Enum;
using Lumps;
using Lumps.BspLumps;
using Lumps.GameLumps;
using Newtonsoft.Json;
using NLog;

public sealed class GameLumpWriter(GameLump gameLump, Stream output, IoHandler? handler, DesiredCompression compression)
    : LumpWriter(output)
{
    [JsonIgnore]
    private readonly GameLump _gameLump = gameLump;

    public List<(Lump, LumpHeaderInfo)> HeaderInfo { get; set; } = [];

    [JsonIgnore]
    protected override IoHandler? Handler { get; set; } = handler;

    [JsonIgnore]
    protected override DesiredCompression Compression { get; set; } = compression;

    private long LumpDataStart { get; set; }

    private long LumpDataEnd { get; set; }

    private readonly long _startPos = output.Position;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void Save()
    {
        // The last gamelump header should be 0 so we add a empty lump at the end
        _gameLump.Lumps.TryAdd(0, null);

        LumpDataStart = BaseStream.Position +
                        4 /* gamelump count 32bit int */ +
                        (_gameLump.Lumps.Count * GameLumpHeader.StructureSize);
        List<GameLumpHeader> headers = WriteAllLumps();
        WriteHeader(headers);
    }

    private void WriteHeader(List<GameLumpHeader> headers)
    {
        Seek((int)_startPos, SeekOrigin.Begin);
        Write(_gameLump.Lumps.Count);
        foreach (GameLumpHeader header in headers)
        {
            Write(header.Id);
            Write(header.Flags);
            Write(header.Version);
            Write(header.FileOfs);
            Write(header.FileLen);
        }

        if (headers.Count != 0 && BaseStream.Position != LumpDataStart)
            throw new InvalidDataException("Failed to write GameLump header: bad length");

        BaseStream.Seek(LumpDataEnd, SeekOrigin.Begin);
    }

    private List<GameLumpHeader> WriteAllLumps()
    {
        Seek((int)LumpDataStart, SeekOrigin.Begin);

        List<GameLumpHeader> headers = [];
        foreach ((GameLumpType key, Lump? lump) in _gameLump.Lumps)
        {
            if (key == 0 || lump is null)
                continue;

            LumpHeaderInfo newHeaderInfo = Write(lump);
            HeaderInfo.Add((lump, newHeaderInfo));

            // TODO: meh
            lump.Version = lump is Sprp sprp
                ? (ushort)sprp.StaticProps.GetVersion()
                : (ushort)lump.Version;

            headers.Add(new GameLumpHeader(newHeaderInfo, (ushort)lump.Version, (int)key));

            Logger.Debug($"Wrote gamelump {key} {(int)key}".PadRight(48) +
                         $"offset: {newHeaderInfo.Offset}".PadRight(24) +
                         $"length: {newHeaderInfo.Length}".PadRight(24));
        }

        if (headers.Count != 0 && headers.Last().Id != 0)
            headers.Add(new GameLumpHeader());

        LumpDataEnd = (int)BaseStream.Position;

        return headers;
    }
}
