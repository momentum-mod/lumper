namespace Lumper.Lib.Bsp.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.Lumps;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Newtonsoft.Json;
using NLog;

public sealed class BspFileReader(BspFile file, Stream input, IoHandler? handler) : LumpReader(input)
{
    [JsonIgnore]
    private readonly BspFile _bsp = file;

    [JsonProperty]
    public IReadOnlyDictionary<BspLumpType, LumpHeaderInfo> Headers =>
        Lumps.ToDictionary(x => x.Item1 is Lump<BspLumpType> lump ? lump.Type : BspLumpType.Unknown, x => x.Item2);

    protected override IoHandler? Handler { get; set; } = handler;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public bool Load()
    {
        if (Handler?.Cancelled ?? false)
            return false;
        Lumps.Clear();
        _bsp.Lumps.Clear();

        if (Handler?.Cancelled ?? false)
            return false;
        Handler?.UpdateProgress(0, "Parsing header");
        ReadHeader();

        if (Handler?.Cancelled ?? false)
            return false;
        Handler?.UpdateProgress((float)IoHandler.ReadProgressProportions.Header, "Loading lumps");
        LoadAll();

        // Not doing progress updates for last stuff, runs instantly compared to IO work.
        if (Handler?.Cancelled ?? false)
            return false;
        ResolveTexNames();
        ResolveTexData();

        return true;
    }

    protected override void ReadHeader()
    {
        if (BaseStream.Position != 0)
            BaseStream.Seek(0, SeekOrigin.Begin);

        byte[] ident = ReadBytes(4);

        if (BspFile.Encoding.GetString(ident) != "VBSP")
            throw new InvalidDataException("File doesn't look like a VBSP");

        _bsp.Version = ReadInt32();
        Logger.Debug($"BSP version: {_bsp.Version}");

        for (int i = 0; i < BspFile.HeaderLumps; i++)
        {
            var type = (BspLumpType)i;

            Lump<BspLumpType> lump = type switch
            {
                BspLumpType.Entities => new EntityLump(_bsp),
                BspLumpType.Texinfo => new TexInfoLump(_bsp),
                BspLumpType.Texdata => new TexDataLump(_bsp),
                BspLumpType.TexdataStringTable => new TexDataStringTableLump(_bsp),
                BspLumpType.TexdataStringData => new TexDataStringDataLump(_bsp),
                BspLumpType.Pakfile => new PakfileLump(_bsp),
                BspLumpType.GameLump => new GameLump(_bsp),
                _ => new UnmanagedLump<BspLumpType>(_bsp),
            };

            LumpHeaderInfo lumpHeaderInfo = new();

            lump.Type = type;
            lumpHeaderInfo.Offset = ReadInt32();
            int length = ReadInt32();
            lump.Version = ReadInt32();
            int fourCc = ReadInt32();
            if (fourCc == 0)
            {
                lumpHeaderInfo.CompressedLength = -1;
                lumpHeaderInfo.UncompressedLength = length;
            }
            else
            {
                lumpHeaderInfo.CompressedLength = length;
                lumpHeaderInfo.UncompressedLength = fourCc;
            }

            Logger.Debug(
                $"Read lump {type} ({(int)type})".PadRight(48)
                    + $"offset: {lumpHeaderInfo.Offset}".PadRight(24)
                    + $"length: {length}".PadRight(24)
                    + $"version: {lump.Version}".PadRight(20)
                    + $"fourCC: {fourCc}"
            );

            _bsp.Lumps.Add(type, lump);
            Lumps.Add(new Tuple<Lump, LumpHeaderInfo>(lump, lumpHeaderInfo));
        }

        _bsp.Revision = ReadInt32();

        UpdateGameLumpLength();

        SortLumps();

        CheckOverlapping();
    }

    // Finding the real gamelump length by looking at the next lump
    private void UpdateGameLumpLength()
    {
        Lump? gameLump = null;
        LumpHeaderInfo? gameLumpHeader = null;
        foreach ((Lump? lump, LumpHeaderInfo? header) in Lumps.OrderBy(x => x.Item2.Offset))
        {
            if (lump is GameLump)
            {
                gameLump = lump;
                gameLumpHeader = header;
                if (gameLumpHeader is { Length: 0, Offset: 0 })
                {
                    Logger.Warn("GameLump length and offset 0 .. won't set new length");
                    break;
                }
            }
            // Iteration where this is true will be the first non-empty lump after the gamelump
            else if (gameLump is not null && header.Offset != 0 && header.Offset != gameLumpHeader!.Offset)
            {
                gameLumpHeader.UncompressedLength = header.Offset - gameLumpHeader.Offset;
                Logger.Debug($"Changed gamelump length to {gameLumpHeader.Length}");
                break;
            }
        }
    }

    // Sort by offset so the output file looks more like the input
    private void SortLumps()
    {
        Dictionary<BspLumpType, Lump<BspLumpType>> newLumps = [];
        foreach ((Lump? lump, _) in Lumps.OrderBy(x => x.Item2.Offset))
        {
            (BspLumpType key, Lump<BspLumpType>? value) = _bsp.Lumps.First(x => x.Value == lump);
            newLumps.Add(key, value);
            _bsp.Lumps.Remove(key);
        }

        if (_bsp.Lumps.Count != 0)
            throw new InvalidDataException("SortLumps error: BSP lumps and reader headers didn't match!");
        _bsp.Lumps = newLumps;
    }

    // Test for overlapping offsets
    private bool CheckOverlapping()
    {
        bool result = false;

        Lump<BspLumpType>? prevLump = null;
        LumpHeaderInfo? prevHeader = null;

        bool first = true;
        foreach ((Lump? tmpLump, LumpHeaderInfo? header) in Lumps.OrderBy(x => x.Item2.Offset))
        {
            var lump = (Lump<BspLumpType>)tmpLump;
            if (first)
            {
                first = false;
                prevLump = lump;
                prevHeader = header;
            }
            else if (header.Length > 0)
            {
                long prevEnd = prevHeader!.Offset + prevHeader.Length;
                if (header.Offset < prevEnd)
                {
                    Logger.Warn(
                        $"Lumps {prevLump!.Type} and {lump.Type} are overlapping (see https://github.com/momentum-mod/lumper/wiki/Overlapping-Lumps)"
                    );
                    if (prevLump.Type == BspLumpType.GameLump)
                        Logger.Warn("but the previous lump was GAME_LUMP and the length is a lie");
                    else
                        result = true;
                }
                else if (header.Offset > prevEnd)
                {
                    Logger.Debug(
                        $"Space between lumps {prevLump!.Type} {prevEnd} <-- {header.Offset - prevEnd} --> {header.Offset} {lump.Type}"
                    );
                }

                if (header.Offset + header.Length >= prevEnd)
                {
                    prevLump = lump;
                    prevHeader = header;
                }
            }
        }

        return result;
    }

    private void ResolveTexNames()
    {
        TexDataLump? texDataLump = _bsp.GetLump<TexDataLump>();
        TexDataStringTableLump? texDataStringTableLump = _bsp.GetLump<TexDataStringTableLump>();
        TexDataStringDataLump? texDataStringDataLump = _bsp.GetLump<TexDataStringDataLump>();

        if (texDataLump == null || texDataStringTableLump == null || texDataStringDataLump == null)
            return;

        foreach (TexData texture in texDataLump.Data)
        {
            int stringTableOffset = texDataStringTableLump.Data[texture.StringTablePointer];

            int end = Array.FindIndex(texDataStringDataLump.Data, stringTableOffset, x => x == 0);
            if (end < 0)
            {
                end = texDataStringDataLump.Data.Length;
                Logger.Warn($"Didn't find null at the end of texture string! ({texture.TexName})");
            }

            texture.TexName =
                end > 0
                    ? BspFile.Encoding.GetString(texDataStringDataLump.Data, stringTableOffset, end - stringTableOffset)
                    : "";
        }
    }

    private void ResolveTexData()
    {
        TexInfoLump? texInfoLump = _bsp.GetLump<TexInfoLump>();
        TexDataLump? texDataLump = _bsp.GetLump<TexDataLump>();

        if (texInfoLump == null || texDataLump == null)
            return;

        foreach (TexInfo texInfo in texInfoLump.Data)
        {
            if (texInfo.TexDataPointer >= 0)
                texInfo.TexData = texDataLump.Data[texInfo.TexDataPointer];
        }
    }
}
