namespace Lumper.Lib.BSP.Lumps;
using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

// Needed in the LumpReader/LumpWriter where we don't have the lump type
public interface IUnmanagedLump
{
    public bool Compressed { get; set; }
    public long UncompressedLength { get; set; }
    public long DataStreamOffset { get; set; }
}

/// <summary>
/// Stores a buffer of some lump data we're not parsing for writing out on save
/// </summary>
public class UnmanagedLump<T>(BspFile parent) : Lump<T>(parent), IUnmanagedLump where T : Enum
{
    public bool Compressed { get; set; }
    public long UncompressedLength { get; set; }

    [JsonIgnore]
    public Stream? DataStream { get; set; }

    public byte[] HashMd5 { get; private set; } = null!;
    public long DataStreamOffset { get; set; }
    public long DataStreamLength { get; set; }
    public static readonly int LzmaId = ('A' << 24) | ('M' << 16) | ('Z' << 8) | ('L');

    public override void Read(BinaryReader reader, long length)
    {
        DataStream = reader.BaseStream;
        DataStreamOffset = reader.BaseStream.Position;
        DataStreamLength = length;

        DataStream.Seek(DataStreamOffset, SeekOrigin.Begin);
        var buffer = new byte[DataStreamLength];
        DataStream.Read(buffer, 0, buffer.Length);
        HashMD5 = MD5.HashData(buffer);
    }

    public override void Write(Stream stream)
    {
        if (DataStream == null)
            return;

        var startPos = DataStream.Position;
        DataStream.Seek(DataStreamOffset, SeekOrigin.Begin);
        var buffer = new byte[1024 * 80];
        int read;
        var remaining = (int)DataStreamLength;
        while ((read = DataStream.Read(buffer, 0, Math.Min(buffer.Length, remaining))) > 0)
        {
            stream.Write(buffer, 0, read);
            remaining -= read;
        }
        DataStream.Seek(startPos, SeekOrigin.Begin);
    }


    public override bool Empty => _buffer is not { Length: > 0 };
}
