using System;
using System.IO;
using System.Linq;
using System.Text;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Lump;

namespace MomBspTools.Lib.BSP
{
    public sealed class BspFileReader : IDisposable
    {
        private BspFile _bspFile;
        private FileStream _stream;

        public BspFileReader(BspFile file)
        {
            _bspFile = file;
            _stream = File.OpenRead(_bspFile.FilePath);
        }

        public void LoadHeader()
        {
            if (_stream.Position != 0) Seek(0);

            using var reader = GetBinaryReader();

            var ident = reader.ReadBytes(4);
            if (Encoding.Default.GetString(ident) != "VBSP") throw new InvalidDataException();

            _bspFile.Version = reader.ReadInt32();

            for (var i = 0; i < BspFile.HeaderLumps; i++)
            {
                var type = (LumpType) i;

                AbstractLump lump = type switch
                {
                    LumpType.LUMP_TEXINFO => new TexInfoLump(_bspFile),
                    LumpType.LUMP_TEXDATA => new TexDataLump(_bspFile),
                    LumpType.LUMP_TEXDATA_STRING_TABLE => new TexDataStringTableLump(_bspFile),
                    LumpType.LUMP_TEXDATA_STRING_DATA => new TexDataStringDataLump(_bspFile),
                    _ => new GenericLump(_bspFile)
                };

                lump.Type = type;
                lump.Offset = reader.ReadInt32();
                lump.Length = reader.ReadInt32();
                lump.Version = reader.ReadInt32();
                lump.FourCc = reader.ReadInt32();

                _bspFile.Lumps.Add(lump);
            }

            _bspFile.Revision = reader.ReadInt32();
        }

        public void LoadAllLumps()
        {
            foreach (var lump in _bspFile.Lumps.Where(lump => lump.GetType() != typeof(GenericLump)))
            {
                LoadLump(lump);
            }
        }

        private void LoadLump(AbstractLump lump)
        {
            if (lump.Length == 0) return;

            Seek(lump.Offset);

            using var reader = GetBinaryReader();

            // If lump has contains non-trivial data structure, its Read() method loads one structure per call.
            // Otherwise, Read() loads everything in a single call.
            if (lump.DataSize > 1)
            {
                var structCount = lump.Length / lump.DataSize;

                for (var i = 0; i < structCount; i++)
                {
                    lump.Read(reader);
                }
            }
            else
            {
                lump.Read(reader);
            }
        }

        public MemoryStream GetLumpStream(AbstractLump lump)
        {
            MemoryStream lumpStream = new();

            _stream.Seek(lump.Offset, 0);
            _stream.CopyTo(lumpStream, lump.Length);

            return lumpStream;
        }

        private BinaryReader GetBinaryReader() => new(_stream, Encoding.Default, true);

        private void Seek(int p) => _stream.Seek(p, 0);

        public void Dispose()
        {
        }
    }
}