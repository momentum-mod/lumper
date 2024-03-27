namespace Lumper.Lib.BSP.Lumps.BspLumps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.BSP.Struct;
using Newtonsoft.Json;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;

//[JsonConverter(typeof(PakFileJsonConverter))]
public class PakFileLump : ManagedLump<BspLumpType>
{
    public List<PakFileEntry> Entries { get; set; } = [];

    private ZipArchive _zip = null!;

    [JsonIgnore]
    public ZipArchive Zip
    {
        get => _zip;
        set
        {
            _zip = value;
            UpdateEntries();
        }
    }

    public PakFileLump(BspFile parent) : base(parent) => Compress = false;


    public override void Read(BinaryReader reader, long length)
    {
        Stream stream = reader.BaseStream;
        var dataStream = new MemoryStream();
        int read;
        var buffer = new byte[80 * 1024];

        var remaining = length;
        while ((read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining))) > 0)
        {
            dataStream.Write(buffer, 0, read);
            remaining -= read;
        }

        dataStream.Seek(0, SeekOrigin.Begin);
        Zip = ZipArchive.Open(dataStream);
    }

    private void UpdateEntries() => Entries = Zip.Entries.Select(entry => new PakFileEntry(entry)).ToList();

    private void UpdateZip(bool closeStream)
    {
        // Delete every item from the zip that's not in the PakLump entries
        foreach (ZipArchiveEntry entry in Zip.Entries.Where(entry => Entries.All(x => x.Key != entry.Key)))
            Zip.RemoveEntry(entry);

        foreach (PakFileEntry entry in Entries)
        {
            // Count entries already in the zip
            var entriesInZip = Zip.Entries.Where(x => x.Key == entry.Key).ToList();
            switch (entriesInZip.Count)
            {
                // If we have an unmodified entry continue on, otherwise delete it
                // so we can insert an updated version.
                case 1:
                    if (!entry.IsModified)
                        continue;
                    Zip.RemoveEntry(entriesInZip.First());
                    break;
                // More than one match somehow - you done fucked it
                case 2:
                    throw new NotSupportedException(
                        $"Paklump has multiple entries of the same key: {entriesInZip.First().Key}");
                default:
                    break;
            }

            Stream stream = entry.DataStream;
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            else if (!entry.IsModified)
            {
                var mem = new MemoryStream();
                stream.CopyTo(mem);
                stream = mem;
            }
            else if (entry.IsModified && stream.Position >= stream.Length)
            {
                throw new EndOfStreamException(
                    "Stream is not seekable, was modified  " +
                    "and there is nothing to read .. what did you do?");
            }

            Zip.AddEntry(entry.Key, stream, closeStream);
        }
    }

    private MemoryStream SaveZip()
    {
        var dataStream = new MemoryStream();
        CompressionType compressionType = Compress
            ? CompressionType.LZMA
            : CompressionType.None;
        Zip.SaveTo(dataStream, new WriterOptions(compressionType));
        return dataStream;
    }

    public override void Write(Stream stream)
    {
        UpdateZip(true);
        using Stream dataStream = SaveZip();
        dataStream.Seek(0, SeekOrigin.Begin);
        dataStream.CopyTo(stream);
    }

    public override bool Empty() => Entries.Count == 0;
}
