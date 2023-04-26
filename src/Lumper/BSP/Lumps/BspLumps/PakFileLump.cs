using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace Lumper.Lib.BSP.Lumps.BspLumps
{
    //[JsonConverter(typeof(PakFileJsonConverter))]
    public class PakFileLump : ManagedLump<BspLumpType>
    {
        [JsonIgnore]
        public MemoryStream DataStream { get; set; }

        [JsonConverter(typeof(ZipJsonConverter))]
        public ZipArchive Zip
        {
            get => GetZipArchive();
        }
        public byte[] HashMD5
        {
            get
            {
                DataStream.Seek(0, SeekOrigin.Begin);
                return MD5.Create().ComputeHash(DataStream);
            }
        }
        public override void Read(BinaryReader reader, long lenght)
        {
            Stream stream = reader.BaseStream;
            DataStream = new MemoryStream();
            int read;
            var buffer = new byte[80 * 1024];

            long remaining = lenght;
            while ((read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining))) > 0)
            {
                DataStream.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        public override void Write(Stream stream)
        {
            if (Compress)
                throw new InvalidDataException("Don't compress the lump containing a zip .. set the zip compression while _writing_ the zip instead");
            DataStream.Seek(0, SeekOrigin.Begin);
            DataStream.CopyTo(stream);
        }

        public override bool Empty()
        {
            return DataStream is null || DataStream.Length <= 0;
        }

        public PakFileLump(BspFile parent) : base(parent)
        {
            Compress = false;
        }
        public ZipArchive GetZipArchive()
        {
            DataStream.Seek(0, SeekOrigin.Begin);
            return ZipArchive.Open(DataStream);
        }

        public void SetZipArchive(ZipArchive zip, bool compress)
        {
            var temp = new MemoryStream();
            var compressionType = compress
                                    ? SharpCompress.Common.CompressionType.LZMA
                                    : SharpCompress.Common.CompressionType.None;
            zip.SaveTo(temp, new SharpCompress.Writers.WriterOptions(compressionType));
            DataStream = temp;
        }
    }

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

}