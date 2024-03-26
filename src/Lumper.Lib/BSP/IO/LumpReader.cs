namespace Lumper.Lib.BSP.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lumper.Lib.BSP.Lumps;
using Newtonsoft.Json;
using SharpCompress.Compressors.LZMA;

// Handles decompressing and fills lumps with data
[JsonObject(MemberSerialization.OptIn)]
public abstract class LumpReader(Stream input) : BinaryReader(input)
{
    // Lump header information is only needed in the reader
    protected List<Tuple<Lump, LumpHeaderInfo>> Lumps { get; set; } = [];

    protected abstract void ReadHeader();

    protected virtual void LoadAll()
    {
        foreach ((Lump lump, LumpHeader lumpHeader) in Lumps)
        {
            if (lumpHeader.Length > 0)
                Read(lump, lumpHeader);
        }
    }

    private MemoryStream Decompress()
    {
        MemoryStream decompressedStream = new();

        const string magic = "LZMA";
        var id = ReadBytes(magic.Length);
        if (Encoding.ASCII.GetString(id) != magic)
            throw new InvalidDataException("Failed to decompress: Lump doesn't look like it was LZMA compressed");

        var actualSize = ReadUInt32();
        var lzmaSize = ReadUInt32();
        var properties = new byte[5];

        Read(properties, 0, 5);

        var lzmaStream = new LzmaStream(properties,
            BaseStream,
            lzmaSize,
            actualSize);
        lzmaStream.CopyTo(decompressedStream);
        decompressedStream.Flush();
        decompressedStream.Seek(0, SeekOrigin.Begin);

        return decompressedStream;
    }

    protected void Read(Lump lump, LumpHeaderInfo lhi)
    {
        BinaryReader reader;
        long lumpStreamLength;

        BaseStream.Seek(lhi.Offset, SeekOrigin.Begin);

        if (lump is IUnmanagedLump unmanagedLump)
        {
            unmanagedLump.Compressed = lumpHeader.Compressed;
            unmanagedLump.UncompressedLength = lumpHeader.Compressed
                ? lumpHeader.UncompressedLength
                : -1;

            lumpReader = this;
            lumpStreamLength = lumpHeader.Length;
        }
        else if (lhi.Compressed)
        {
            MemoryStream decompressedStream = Decompress();
            reader = new BinaryReader(decompressedStream);
            lumpStreamLength = decompressedStream.Length;
        }
        else
        {
            reader = this;
            lumpStreamLength = lhi.UncompressedLength;
        }

        lump.Read(lumpReader, lumpStreamLength);
        lumpReader.BaseStream.Seek(startPos, SeekOrigin.Begin);
    }

    public void ToJson(Stream stream)
    {
        try
        {
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };
            using var sw = new StreamWriter(stream);
            using var writer = new JsonTextWriter(sw);
            serializer.Serialize(writer,
                Lumps.Select(x => new
                {
                    Header = x.Item2,
                    Lump = x.Item1
                }));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

}
