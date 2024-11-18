namespace Lumper.Lib.BSP.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Enum;
using Lumps;
using Lumps.BspLumps;
using Newtonsoft.Json;
using NLog;
using SharpCompress.Compressors.LZMA;

/// <summary>
/// Handles decompressing and fills lumps with data
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public abstract class LumpReader(Stream input) : BinaryReader(input, encoding: Encoding.UTF8, leaveOpen: true)
{
    public List<Tuple<Lump, LumpHeaderInfo>> Lumps { get; set; } = [];

    protected abstract IoHandler? Handler { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected abstract void ReadHeader();

    protected virtual void LoadAll()
    {
        const float incr = (float)IoHandler.ReadProgressProportions.OtherLumps / BspFile.HeaderLumps;

        foreach ((Lump lump, LumpHeaderInfo lumpHeader) in Lumps)
        {
            if (Handler?.Cancelled ?? false)
                return;

            if (this is BspFileReader)
                Handler?.UpdateProgress(incr, $"Reading {((Lump<BspLumpType>)lump).Type}");

            if (lumpHeader.Length > 0)
                Read(lump, lumpHeader);
        }
    }

    protected void Read(Lump lump, LumpHeaderInfo lumpHeader)
    {
        BinaryReader reader;
        long lumpStreamLength;

        BaseStream.Seek(lumpHeader.Offset, SeekOrigin.Begin);

        lump.IsCompressed = lump is not GameLump && lump is not PakfileLump && lumpHeader.Compressed;
        if (lump is IUnmanagedLump unmanagedLump)
        {
            unmanagedLump.UncompressedLength = lumpHeader.Compressed ? lumpHeader.UncompressedLength : -1;
            reader = this;
            lumpStreamLength = lumpHeader.Length;
        }
        else if (lumpHeader.Compressed)
        {
            MemoryStream decompressedStream = Decompress();
            reader = new BinaryReader(decompressedStream);
            lumpStreamLength = decompressedStream.Length;
        }
        else
        {
            reader = this;
            lumpStreamLength = lumpHeader.UncompressedLength;
        }

        var startPos = reader.BaseStream.Position;

        // Pass handler to pakfile lump so can update progress
        if (lump is PakfileLump pakfileLump)
            pakfileLump.Read(reader, lumpStreamLength, Handler);
        else if (lump is GameLump gameLump)
            gameLump.Read(reader, lumpStreamLength, Handler);
        else
            lump.Read(reader, lumpStreamLength, Handler);

        reader.BaseStream.Seek(startPos, SeekOrigin.Begin);
    }

    public void ToJson(Stream stream)
    {
        try
        {
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };
            using var sw = new StreamWriter(stream);
            using var writer = new JsonTextWriter(sw);
            serializer.Serialize(writer, Lumps.Select(x => new { Header = x.Item2, Lump = x.Item1 }));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to serialize to JSON");
        }
    }

    private MemoryStream Decompress()
    {
        MemoryStream decompressedStream = new();

        const string magic = "LZMA";
        if (Encoding.ASCII.GetString(ReadBytes(magic.Length)) != magic)
            throw new InvalidDataException("Failed to decompress: Lump doesn't look like it was LZMA compressed");

        var actualSize = ReadUInt32();
        var lzmaSize = ReadUInt32();
        var properties = new byte[5];

        _ = Read(properties, 0, 5);

        var lzmaStream = new LzmaStream(properties, BaseStream, lzmaSize, actualSize);
        lzmaStream.CopyTo(decompressedStream);
        decompressedStream.Flush();
        decompressedStream.Seek(0, SeekOrigin.Begin);

        return decompressedStream;
    }
}
