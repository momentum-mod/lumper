namespace Lumper.Lib.BSP.Lumps;

using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using Bsp.Enum;
using IO;
using Newtonsoft.Json;

// Needed in the LumpReader/LumpWriter where we don't have the lump type
public interface IUnmanagedLump : IFileBackedLump
{
    public long UncompressedLength { get; set; }
}

/// <summary>
/// Stores a buffer of some lump data we're not parsing for writing out on save
/// </summary>
public class UnmanagedLump<T>(BspFile parent) : Lump<T>(parent), IUnmanagedLump where T : Enum
{
    [JsonIgnore]
    public Stream DataStream { get; set; } = null!;

    public long UncompressedLength { get; set; }

    public long DataStreamOffset { get; set; }

    public long DataStreamLength { get; set; }

    public byte[] HashSha1 { get; private set; } = null!;

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
    {
        var originalOffset = reader.BaseStream.Position;
        DataStreamLength = length;

        var buffer = new byte[length];
        reader.BaseStream.ReadExactly(buffer, 0, (int)length);
        HashSha1 = SHA1.HashData(buffer);

        if (Parent.FileStream is not null)
        {
            DataStream = Parent.FileStream;
            DataStreamOffset = originalOffset;
            return;
        }

        DataStream = new MemoryStream(buffer);
        DataStreamOffset = 0;
    }

    public override void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null)
    {
        DataStream.Seek(DataStreamOffset, SeekOrigin.Begin);

        var buffer = ArrayPool<byte>.Shared.Rent(80 * 1024);
        try
        {
            int read;
            var remaining = (int)DataStreamLength;
            while ((read = DataStream.Read(buffer, 0, int.Min(buffer.Length, remaining))) > 0)
            {
                stream.Write(buffer, 0, read);
                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public override bool Empty => DataStream is not { Length: > 0 };
}
