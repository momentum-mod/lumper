using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpCompress.Compressors.LZMA;
using Lumper.Lib.BSP.Lumps;

namespace Lumper.Lib.BSP.IO
{
    // handles decompressing and fills lumps with data
    public abstract class LumpReader : BinaryReader
    {
        // lumpheader information is only needed in the reader
        protected List<Tuple<Lump, LumpHeader>> Lumps = new();
        public LumpReader(Stream input) : base(input)
        { }
        protected abstract void ReadHeader();
        protected virtual void LoadAll()
        {
            foreach (var l in Lumps)
            {
                var lump = l.Item1;
                var lumpHeader = l.Item2;
                if (lumpHeader.Length > 0)
                    Read(lump, lumpHeader);
            }
        }
        private MemoryStream Decompress()
        {
            MemoryStream decompressedStream = new();

            const string magic = "LZMA";
            var id = ReadBytes(magic.Length);
            if (Encoding.ASCII.GetString(id) != magic)
                throw new InvalidDataException("Failed to decompress: Lump doesn't look like it was LZMA compressed");

            uint actualSize = ReadUInt32();
            uint lzmaSize = ReadUInt32();
            byte[] properties = new byte[5];

            Read(properties, 0, 5);

            var lzmaStream = new LzmaStream(properties,
                                            BaseStream,
                                            lzmaSize,
                                            actualSize);
            lzmaStream.CopyTo(decompressedStream);
            decompressedStream.Flush();
            decompressedStream.Seek(0, SeekOrigin.Begin);

            return decompressedStream;
        }
        protected void Read(Lump lump, LumpHeader lumpHeader)
        {
            BinaryReader lumpReader;
            long lumpStreamLength;

            BaseStream.Seek(lumpHeader.Offset, SeekOrigin.Begin);

            if (lump is IUnmanagedLump unmanagedLump)
            {
                unmanagedLump.Compressed = lumpHeader.Compressed;
                unmanagedLump.UncompressedLength = lumpHeader.Compressed
                                                ? lumpHeader.UncompressedLength
                                                : -1;

                lumpReader = this;
                lumpStreamLength = lumpHeader.Length;
            }
            else if (lumpHeader.Compressed)
            {
                MemoryStream decompressedStream = Decompress();
                lumpReader = new BinaryReader(decompressedStream);
                lumpStreamLength = decompressedStream.Length;
            }
            else
            {
                lumpReader = this;
                lumpStreamLength = lumpHeader.UncompressedLength;
            }

            var startPos = lumpReader.BaseStream.Position;
            lump.Read(lumpReader, lumpStreamLength);
            lumpReader.BaseStream.Seek(startPos, SeekOrigin.Begin);
        }
    }
}