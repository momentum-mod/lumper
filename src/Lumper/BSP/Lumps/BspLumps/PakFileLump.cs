using System;
using System.IO;
using SharpCompress.Archives.Zip;

namespace Lumper.Lib.BSP.Lumps.BspLumps
{
    public class PakFileLump : ManagedLump<BspLumpType>
    {
        public MemoryStream DataStream { get; set; }
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
}