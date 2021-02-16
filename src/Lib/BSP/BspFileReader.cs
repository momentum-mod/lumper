using System;
using System.IO;
using System.Linq;
using System.Text;
using MomBspTools.Lib.BSP.Lumps;

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

        public BinaryReader GetBinaryReader() => new BinaryReader(_stream, Encoding.Default, true);
        
        public void LoadHeader()
        {
            if (_stream.Position != 0) _stream.Seek(0, 0);

            using var reader = GetBinaryReader();

            var ident = reader.ReadBytes(4);
            if (Encoding.Default.GetString(ident) != "VBSP") throw new InvalidDataException();

            _bspFile.Version = reader.ReadInt32();

            for (var i = 0; i < BspFile.HeaderLumps; i++)
            {
                var type = (LumpType) i;
                
                Lump lump = type switch
                {
                    LumpType.LUMP_TEXDATA => new TexDataLump(),
                    _ => new GenericLump()
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

        private void LoadLump(Lump lump)
        {
            if (lump.Length == 0) return;

            Seek(lump.Offset);

            using var reader = GetBinaryReader();

            var structSize = lump.DataSize;
            var structCount = lump.Length / structSize;

            for (var i = 0; i < structCount; i++)
            {
                lump.Read(reader);
            }
        }

        public MemoryStream GetLumpStream(Lump lump)
        {
            MemoryStream lumpStream = new();
            
            _stream.Seek(lump.Offset, 0);
            _stream.CopyTo(lumpStream, lump.Length);
            
            return lumpStream;
        }

        private void Seek(int p) => _stream.Seek(p, 0);

        public void Dispose() {  }
    }
}