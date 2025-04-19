namespace Lumper.Lib.Bsp.Struct;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Newtonsoft.Json;
using SharpCompress.Archives.Zip;

/// <summary>
/// A pakfile entry derived from either a ZipArchiveEntry or just a stream.
///
/// SharpCompress imposes several annoying constraints that makes zip archive handling very difficult.
/// - You can't change the key of a ZipArchiveEntry, have to delete then re-add
/// - ZipEntry streams are single-threaded (we encounter very weird behavior when trying to read during multiple threads)
///
/// To load a zip entry into memory, call GetData or PrefetchData. This is *SLOW*, and access is locked to a single
/// thread.
///
/// The zip archive itself is only written on save, in PakfileLump.Write.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public sealed class PakfileEntry
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

        // Initing this from a stream assumes we're *not* using a zip entry, so we're modifying
        // the paklump.
        IsModified = true;

        using var mem = new MemoryStream();
        stream.CopyTo(mem);
        _buffer = mem.GetBuffer();
    }

    // Probably shouldn't be public but whatever. be careful!
    public ZipArchiveEntry? ZipEntry { get; set; }

    [JsonProperty]
    // This setter needs to be exposed, but you almost certainly shouldn't use it; use Rename() instead!
    public string Key { get; set; }

    [JsonProperty]
    public long? CompressedSize => ZipEntry?.CompressedSize ?? null;

    private readonly PakfileLump _parent;

    private bool _isModified;
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

    private ReadOnlyMemory<byte>? _buffer;

    // Static lock for access to zip archive - SharpCompress's OpenEntryStream is not thread-safe.
    private static readonly Lock ZipAccessLock = new();

    public void PrefetchData()
    {
        if (_buffer is not null)
            return;

        lock (ZipAccessLock)
        {
            int size = (int)ZipEntry!.Size;
            using var mem = new MemoryStream(size); // Note MemoryStream disposal doesn't delete underlying buffer
            using Stream zipStream = ZipEntry!.OpenEntryStream();
            zipStream.CopyTo(mem);
            _buffer = mem.GetBuffer().AsMemory(0, size);
        }
    }

    /// <summary>
    /// Retrieve a ReadonlySpan of the entry data, reading from the pakfile if it hasn't been read already.
    /// </summary>
    public ReadOnlySpan<byte> GetData()
    {
        if (_buffer is not null)
            return _buffer!.Value.Span;

        if (ZipEntry is null)
            throw new InvalidOperationException("Pakfile entry has no data");

        PrefetchData();

        return _buffer!.Value.Span;
    }

    public void UpdateData(ReadOnlyMemory<byte> data)
    {
        _buffer = data;
        _hash = null;
        IsModified = true;
    }

    public void Rename(string newName)
    {
        Key = newName;
        IsModified = true;

        // Prefetch our data and set ZipEntry to null. During PakfileLump.UpdateZip, this will cause the original
        // entry to be removed, and a new entry with the given key to be added in its place
        PrefetchData();

        ZipEntry = null;
    }

    private string? _hash;

    /// <summary>
    /// Get a SHA1 hash of the current pakfile entry data.
    ///
    /// If the entry hasn't been read from the Pakfile zip already, the contents will be read into
    /// this class's buffer. This is by far the  most expensive part of this method, and only one
    /// zip entry can be read at a time -- do not try to parallelize multiple calls to this method
    /// for multiple entries.
    /// </summary>
    [JsonProperty]
    public string Hash
    {
        get
        {
            if (_hash is not null)
                return _hash;

            _hash = Convert.ToHexString(SHA1.HashData(GetData()));

            return _hash;
        }
    }
}
