namespace Lumper.Lib.BSP.IO;
using System;
using System.IO;
using Lumper.Lib.BSP.Lumps;
using Newtonsoft.Json;
using SharpCompress.Compressors.LZMA;

// Handles compression and writes lump data to a stream
[JsonObject(MemberSerialization.OptIn)]
public abstract class LumpWriter(Stream output) : BinaryWriter(output)
{
    public static readonly int LzmaId = ('A' << 24) | ('M' << 16) | ('Z' << 8) | ('L');

    private long Compress(Stream uncompressedStream)
    {
        long compressedLength;

        uncompressedStream.Seek(0, SeekOrigin.Begin);

        var properties = new LzmaEncoderProperties();
        var mem = new MemoryStream();

        const int headerSize = 4 + 4 + 4 + 5;
        mem.Seek(headerSize, SeekOrigin.Begin);

        var lzmaStream = new LzmaStream(properties, false, mem);
        uncompressedStream.CopyTo(lzmaStream);
        lzmaStream.Flush();
        lzmaStream.Dispose();

        mem.Seek(0, SeekOrigin.Begin);

        var w = new BinaryWriter(mem);
        if (mem.Length > uncompressedStream.Length)
        {
            Console.WriteLine("Compressed lump larger than uncompressed, skipping");

            uncompressedStream.Seek(0, SeekOrigin.Begin);
            uncompressedStream.CopyTo(BaseStream);

            compressedLength = -1;
        }
        else
        {
            mem.Seek(0, SeekOrigin.Begin);
            compressedLength = mem.Length;
            w.Write(LzmaId);
            w.Write((int)uncompressedStream.Length);
            w.Write((int)mem.Length - headerSize);
            w.Write(lzmaStream.Properties);
            if (w.BaseStream.Position != headerSize)
                throw new InvalidDataException("Failed to compress stream: bad LZMA header");

            mem.Seek(0, SeekOrigin.Begin);
            mem.CopyTo(BaseStream);
        }
        return compressedLength;
    }
    public LumpHeader Write(Lump lump)
    {
        var offset = BaseStream.Position;
        long uncompressedLength;
        long compressedLength;

        if (lump is IUnmanagedLump unmanagedLump && unmanagedLump.Compressed)
        {
            if (!lump.Compress)
                Console.WriteLine("UnmanagedLump is compressed but was set to be written uncompressed, writing compressed lump");

            lump.Write(BaseStream);
            uncompressedLength = unmanagedLump.UncompressedLength;
            compressedLength = BaseStream.Position - offset;
        }
        else
        {
            if (lump.Compress)
            {
                var uncompressedStream = new MemoryStream();
                lump.Write(uncompressedStream);

                uncompressedLength = uncompressedStream.Length;
                compressedLength = Compress(uncompressedStream);
            }
            else
            {
                lump.Write(BaseStream);

                uncompressedLength = BaseStream.Position - offset;
                compressedLength = -1;
            }
        }

        return new LumpHeader(
            offset,
            uncompressedLength,
            compressedLength);
    }
}
