namespace Lumper.Lib.Bsp.IO;

using System.IO;
using System.Text;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.Lumps;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Newtonsoft.Json;
using NLog;
using SharpCompress.Compressors.LZMA;

/// <summary>
/// Handles compression and writes lump data to a stream.
///
/// This writer doesn't close its stream when disposed.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public abstract class LumpWriter(Stream output) : BinaryWriter(output, Encoding.UTF8, leaveOpen: true)
{
    protected abstract IoHandler? Handler { get; set; }

    protected abstract DesiredCompression Compression { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public LumpHeaderInfo Write(Lump lump)
    {
        // Always uncompressed if empty
        if (lump.Empty)
        {
            return new LumpHeaderInfo
            {
                Offset = BaseStream.Position,
                CompressedLength = -1,
                UncompressedLength = 0,
            };
        }

        // Pakfile and gamelump skip this completely, always write uncompressed
        if (lump is PakfileLump or GameLump)
            return WriteUncompressed(lump);

        if (lump is IUnmanagedLump unmanagedLump)
        {
            if (lump.IsCompressed)
            {
                // Debated having a more complex system allowing you to specifically selected whether to leave
                // stuff unchanged or to decompress the lump, but it's hard to imagine a use-case, and makes
                // the UX more complicated. If someone *really* wants to decompress everything for whatever reason,
                // they should just use bspzip.
                if (Compression == DesiredCompression.Uncompressed)
                    Logger.Debug("Saving uncompressed but an unmanaged lump is compressed, leaving it as-is.");

                long offset = BaseStream.Position;

                lump.Write(BaseStream, Handler);

                return new LumpHeaderInfo
                {
                    Offset = offset,
                    UncompressedLength = unmanagedLump.UncompressedLength,
                    CompressedLength = BaseStream.Position - offset,
                };
            }

            if (Compression == DesiredCompression.Compressed)
                return WriteCompressed(lump);

            return WriteUncompressed(lump);
        }

        if (
            Compression == DesiredCompression.Compressed
            || (Compression == DesiredCompression.Unchanged && lump.IsCompressed)
        )
        {
            return WriteCompressed(lump);
        }

        return WriteUncompressed(lump);
    }

    private LumpHeaderInfo WriteUncompressed(Lump lump)
    {
        long offset = BaseStream.Position;

        if (lump is PakfileLump pakfileLump)
            pakfileLump.Write(BaseStream, Handler, Compression);
        else if (lump is GameLump gameLump)
            gameLump.Write(BaseStream, Handler, Compression);
        else
            lump.Write(BaseStream, Handler);

        return new LumpHeaderInfo
        {
            Offset = offset,
            UncompressedLength = BaseStream.Position - offset,
            CompressedLength = -1,
        };
    }

    private LumpHeaderInfo WriteCompressed(Lump lump)
    {
        long offset = BaseStream.Position;

        using var uncompressedStream = new MemoryStream();
        lump.Write(uncompressedStream, Handler);
        long uncompressedLength = uncompressedStream.Length;

        uncompressedStream.Seek(0, SeekOrigin.Begin);

        using var mem = new MemoryStream();

        // Seek past header so we can write contents first, we need
        // to know length we just wrote to set header.
        const int headerSize = 4 + 4 + 4 + 5; // id + actualSize + lzmaSize + properties
        mem.Seek(headerSize, SeekOrigin.Begin);

        var lzmaStream = new LzmaStream(new LzmaEncoderProperties(), false, mem);
        uncompressedStream.CopyTo(lzmaStream);
        lzmaStream.Dispose();

        mem.Seek(0, SeekOrigin.Begin);
        long compressedLength = mem.Length;

        var writer = new BinaryWriter(mem);
        const int lzmaId = ('A' << 24) | ('M' << 16) | ('Z' << 8) | 'L';
        writer.Write(lzmaId);
        writer.Write((int)uncompressedStream.Length);
        writer.Write((int)mem.Length - headerSize);
        writer.Write(lzmaStream.Properties);

        if (writer.BaseStream.Position != headerSize)
            throw new InvalidDataException("Failed to compress stream: bad LZMA header");

        mem.Seek(0, SeekOrigin.Begin);
        mem.CopyTo(BaseStream);

        return new LumpHeaderInfo
        {
            Offset = offset,
            CompressedLength = compressedLength,
            UncompressedLength = uncompressedLength,
        };
    }
}
