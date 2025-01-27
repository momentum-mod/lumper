namespace Lumper.Lib.Bsp.Struct;

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Newtonsoft.Json;
using SharpCompress.Archives.Zip;

/// <summary>
/// A pakfile entry derived from either a ZipArchiveEntry or just a stream.
/// We assume one is non-null throughout.
/// </summary>
public sealed class PakfileEntry : IDisposable
{
    public PakfileEntry(PakfileLump parent, ZipArchiveEntry zipEntry)
    {
        _parent = parent;
        ZipEntry = zipEntry;
        Key = zipEntry.Key ?? throw new InvalidDataException("Pakfile contains an item without a key");
    }

    public PakfileEntry(PakfileLump parent, string key, Stream stream)
    {
        _parent = parent;
        Key = key;

        using var mem = new MemoryStream();
        stream.CopyTo(mem);
        _buffer = mem.GetBuffer();
    }

    // Probably shouldn't be public but whatever. be careful!
    [JsonIgnore]
    public ZipArchiveEntry? ZipEntry { get; }

    public string Key { get; set; }

    [JsonProperty]
    public long? CompressedSize => ZipEntry?.CompressedSize ?? null;

    private readonly PakfileLump _parent;

    [JsonIgnore]
    private bool _isModified;

    [JsonIgnore]
    public bool IsModified
    {
        get => _isModified;
        set
        {
            _isModified = value;
            if (value)
                _parent.IsModified = true;
        }
    }

    [JsonIgnore]
    private byte[]? _buffer;

    [JsonIgnore]
    private readonly List<MemoryStream> _issuedStreams = [];

    [JsonIgnore]
    // Instance lock for access to buffer and issued streams, ensuring original data is read from
    // zip thread-safely, and that new streams are not issued as data is being replaced.
    private readonly Lock _instanceLock = new();

    // Static lock for access to zip archive - SharpCompress's OpenEntryStream is not thread-safe.
    private static readonly Lock ZipAccessLock = new();

    /// <summary>
    /// Get an stream to the uncompressed pakfile entry.
    ///
    /// The returned stream is unwritable, and wraps a single buffer stored on this class.
    /// This allows providing multiple streams which safely can be reading concurrently,
    /// without duplicating the buffer.
    ///
    /// However, the stream can be invalidated if the entry is updated by the user.
    /// </summary>
    public MemoryStream GetReadOnlyStream()
    {
        lock (_instanceLock)
        {
            if (_buffer is not null)
            {
                var stream = new MemoryStream(_buffer!, writable: false);
                _issuedStreams.Add(stream);
                return stream;
            }
            else
            {
                MemoryStream outStream;
                lock (ZipAccessLock)
                {
                    using Stream zipStream = ZipEntry!.OpenEntryStream();
                    outStream = new MemoryStream();
                    zipStream.CopyTo(outStream);
                }

                _buffer = outStream.ToArray();
                outStream.Dispose();

                var stream = new MemoryStream(_buffer!, writable: false);
                _issuedStreams.Add(stream);
                return stream;
            }
        }
    }

    public void UpdateData(byte[] data)
    {
        lock (_instanceLock)
        {
            DisposeIssuedStreams();
            _buffer = new byte[data.Length];
            Array.Copy(data, _buffer, data.Length);
            IsModified = true;
        }
    }

    public void UpdateData(MemoryStream stream)
    {
        lock (_instanceLock)
        {
            DisposeIssuedStreams();
            _buffer = stream.ToArray();
            IsModified = true;
        }
    }

    private void DisposeIssuedStreams()
    {
        foreach (MemoryStream oldStream in _issuedStreams)
            oldStream.Close();
    }

    public void Dispose() => DisposeIssuedStreams();

    // Deliberately not [JsonIgnore]ed so we expose this to JSON dumps.
    public string HashSHA1 => Convert.ToHexString(SHA1.Create().ComputeHash(GetReadOnlyStream()));
}
