using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using SharpCompress.Writers;
using SharpCompress.Common;
using SharpCompress.Archives.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lumper.Lib.BSP.Struct;

namespace Lumper.Lib.BSP.Lumps.BspLumps
{
    //[JsonConverter(typeof(PakFileJsonConverter))]
    public class PakFileLump : ManagedLump<BspLumpType>
    {
        public List<PakFileEntry> Entries { get; set; } = new();

        [JsonIgnore]
        public ZipArchive Zip { get; set; }

        public override void Read(BinaryReader reader, long lenght)
        {
            Stream stream = reader.BaseStream;
            var dataStream = new MemoryStream();
            int read;
            var buffer = new byte[80 * 1024];

            long remaining = lenght;
            while ((read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining))) > 0)
            {
                dataStream.Write(buffer, 0, read);
                remaining -= read;
            }

            dataStream.Seek(0, SeekOrigin.Begin);
            Zip = ZipArchive.Open(dataStream);

            Entries.Clear();
            foreach (var entry in Zip.Entries)
            {
                Entries.Add(new PakFileEntry(entry));
            }
        }

        private void UpdateZip(bool closeStream)
        {
            List<ZipArchiveEntry> deleteList = new();
            foreach (var entry in Zip.Entries)
            {
                if (!Entries.Any(x => x.Key == entry.Key))
                    deleteList.Add(entry);
            }
            foreach (var entry in deleteList)
                Zip.RemoveEntry(entry);

            foreach (var entry in Entries)
            {
                var existing =
                    Zip.Entries.Where(x => x.Key == entry.Key);
                if (!entry.IsModified)
                    continue;
                if (existing.Count() == 1)
                {
                    Zip.RemoveEntry(existing.First());
                }
                else if (existing.Count() > 1)
                {
                    throw new NotImplementedException(
                        "You can have multiple entries" +
                        "with the same name in a zip?");
                }
                var stream = entry.DataStream;
                if (stream.CanSeek)
                    stream.Seek(0, SeekOrigin.Begin);
                /*else if (!entry.IsModified)
                {
                    var mem = new MemoryStream();
                    stream.CopyTo(mem);
                    stream = mem;
                }*/
                else if (entry.IsModified && stream.Position >= stream.Length)
                    throw new EndOfStreamException(
                        "Stream is not seekable, was modified  " +
                        "and there is nothing to read .. what did you do?");
                Zip.AddEntry(entry.Key, stream, closeStream);
            }
        }

        private Stream SaveZip()
        {
            var dataStream = new MemoryStream();
            var compressionType = Compress
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
                using var dataStream = SaveZip();
                dataStream.Seek(0, SeekOrigin.Begin);
                dataStream.CopyTo(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override bool Empty()
        {
            return !Entries.Any();
        }

        public PakFileLump(BspFile parent) : base(parent)
        {
            Compress = false;
        }
    }

    /*
        public class ZipJsonConverter : JsonConverter<ZipArchive>
        {
            public override void WriteJson(JsonWriter writer,
                                           ZipArchive? value,
                                           JsonSerializer serializer)
            {
                if (value is null)
                    return;
                var zip = value;
                writer.WriteStartArray();
                foreach (var entry in zip.Entries)
                {
                    var md5 = MD5.Create().ComputeHash(entry.OpenEntryStream());
                    JObject o = JObject.FromObject(new
                    {
                        entry.Key,
                        MD5 = md5
                    });
                    o.WriteTo(writer);
                }
                writer.WriteEndArray();
            }

            public override ZipArchive? ReadJson(JsonReader reader,
                                                 Type objectType,
                                                 ZipArchive? existingValue,
                                                 bool hasExistingValue,
                                                 JsonSerializer serializer)
            {
                throw new NotImplementedException(
                    "Unnecessary because CanRead is false. " +
                    "The type will skip the converter.");
            }

            public override bool CanRead
            {
                get { return false; }
            }
        }
        */
}