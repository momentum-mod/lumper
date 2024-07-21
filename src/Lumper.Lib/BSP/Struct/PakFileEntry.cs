namespace Lumper.Lib.BSP.Struct;

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SharpCompress.Archives.Zip;

/// <summary>
/// A pakfile entry derived from either a ZipArchiveEntry or just a stream.
/// We assume one is non-null throughout.
/// </summary>
public sealed class PakfileEntry : IDisposable
{
    public PakfileEntry(ZipArchiveEntry zipEntry)
    {
        ZipEntry = zipEntry;
        Key = zipEntry.Key ?? throw new InvalidDataException("Pakfile contains an item without a key");
    }

    public PakfileEntry(string key, Stream stream)
    {
        Key = key;

        stream.Seek(0, SeekOrigin.Begin);
        var mem = new MemoryStream();
        stream.CopyTo(mem);
        _buffer = mem.ToArray();
    }

    // Probably shouldn't be public but whatever. be careful!
    [JsonIgnore]
    public ZipArchiveEntry? ZipEntry { get; }

    public string Key { get; set; }

    [JsonProperty]
    public long? CompressedSize => ZipEntry?.CompressedSize ?? null;

    [JsonIgnore]
    public bool IsModified { get; set; }

    [JsonIgnore]
    private byte[]? _buffer;

    private readonly List<MemoryStream> _issuedStreams = [];

    [JsonIgnore]
    private readonly object _lock = new();

    /// <summary>
    /// Get an stream to the uncompressed pakfile entry.
    ///
    /// This stream can be invalidated if the entry is updated by the user.
    /// </summary>
    public MemoryStream GetReadOnlyStream()
    {
        lock (_lock)
        {
            if (_buffer is not null)
                return IssueStream();

            using Stream zipStream = ZipEntry!.OpenEntryStream();
            using var outStream = new MemoryStream();
            zipStream.CopyTo(outStream);
            _buffer = outStream.ToArray();
            return IssueStream();
        }
    }

    private MemoryStream IssueStream()
    {
        var stream = new MemoryStream(_buffer!, writable: false);
        _issuedStreams.Add(stream);
        return stream;
    }

    public void UpdateData(byte[] data)
    {
        lock (_lock)
        {
            DisposeIssuedStreams();
            _buffer = data;
        }
    }

    private void DisposeIssuedStreams()
    {
        foreach (MemoryStream oldStream in _issuedStreams)
            oldStream.Close();
    }

    public void Dispose() => DisposeIssuedStreams();

    // Doesn't matter that this is unused, is used for JSON comparisons.
    public byte[] HashMd5
    {
        get
        {
            Stream stream = GetReadOnlyStream();
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            return MD5.Create().ComputeHash(stream);
        }
    }
}
