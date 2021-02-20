using System;
using System.IO;
using System.Linq;
using System.Text;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Lumps;

namespace MomBspTools.Lib.BSP
{
    public sealed class BspFileReader : IDisposable
    {
        private readonly BspFile _bspFile;
        private readonly FileStream _stream;
        private readonly BinaryReader _reader;

        public BspFileReader(BspFile file)
        {
            _bspFile = file;
            _stream = File.OpenRead(_bspFile.FilePath);
            _reader = new BinaryReader(_stream);
        }

        public void LoadHeader()
        {
            if (_stream.Position != 0) Seek(0);

            var ident = _reader.ReadBytes(4);
            if (Encoding.Default.GetString(ident) != "VBSP") throw new InvalidDataException();

            _bspFile.Version = _reader.ReadInt32();

            for (var i = 0; i < BspFile.HeaderLumps; i++)
            {
                var type = (LumpType) i;

                Lump lump = type switch
                {
                    LumpType.LUMP_ENTITIES => new EntityLump(_bspFile),
                    LumpType.LUMP_TEXINFO => new TexInfoLump(_bspFile),
                    LumpType.LUMP_TEXDATA => new TexDataLump(_bspFile),
                    LumpType.LUMP_TEXDATA_STRING_TABLE => new TexDataStringTableLump(_bspFile),
                    LumpType.LUMP_TEXDATA_STRING_DATA => new TexDataStringDataLump(_bspFile),
                    _ => new UnmanagedLump(_bspFile)
                };

                lump.Type = type;
                lump.Offset = _reader.ReadInt32();
                lump.Length = _reader.ReadInt32();
                lump.Version = _reader.ReadInt32();
                lump.FourCc = _reader.ReadInt32();

                _bspFile.Lumps.Add(lump);
            }

            _bspFile.Revision = _reader.ReadInt32();
        }

        public void LoadAllLumps()
        {
            foreach (var l in _bspFile.Lumps.Where(lump => lump is ManagedLump))
            {
                var lump = (ManagedLump) l;
                LoadLump(lump);
            }
        }


        private void LoadLump(ManagedLump lump)
        {
            if (lump.Length == 0) return;

            Seek(lump.Offset);

            lump.Read(_reader);
        }

        public MemoryStream GetLumpStream(Lump lump)
        {
            MemoryStream lumpStream = new();

            _stream.Seek(lump.Offset, 0);
            _stream.CopyTo(lumpStream, lump.Length);

            return lumpStream;
        }

        private void Seek(int p) => _stream.Seek(p, 0);

        public void Dispose() => _reader.Dispose();
    }
}