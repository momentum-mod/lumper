namespace Lumper.Lib.BSP.Lumps.BspLumps;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bsp.Enum;
using Enum;
using IO;
using Lumps;
using Newtonsoft.Json;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;
using Struct;

//[JsonConverter(typeof(PakFileJsonConverter))]
public class PakFileLump : ManagedLump<BspLumpType>
{
    public List<PakfileEntry> Entries { get; private set; } = [];

    private ZipArchive _zip;
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

    private void UpdateEntries()
    {
        Entries.Clear();
        foreach (ZipArchiveEntry entry in Zip.Entries)
        {
            Entries.Add(new PakFileEntry(entry));
        }
    }

    private void UpdateZip(bool closeStream)
    {
        List<ZipArchiveEntry> deleteList = [];
        foreach (ZipArchiveEntry entry in Zip.Entries)
        {
            if (!Entries.Any(x => x.Key == entry.Key))
                deleteList.Add(entry);
        }
        foreach (ZipArchiveEntry entry in deleteList)
            Zip.RemoveEntry(entry);

        foreach (PakFileEntry entry in Entries)
        {
            IEnumerable<ZipArchiveEntry> existing =
                Zip.Entries.Where(x => x.Key == entry.Key);
            if (existing.Any() && !entry.IsModified)
                continue;
            if (existing.Count() == 1)
            {
                Zip.RemoveEntry(existing.First());
            }
            else if (existing.Count() > 1)
            {
                throw new NotSupportedException(
                    $"Paklump has multiple entries of the same key: " +
                    "{existing.Key}");
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

    private Stream SaveZip()
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
        try
        {
            UpdateZip(true);
            using Stream dataStream = SaveZip();
            dataStream.Seek(0, SeekOrigin.Begin);
            dataStream.CopyTo(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public override bool Empty() => Entries.Count == 0;

    public PakFileLump(BspFile parent) : base(parent) => Compress = false;
}
