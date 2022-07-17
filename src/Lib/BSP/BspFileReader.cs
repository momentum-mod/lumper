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
        private readonly BspFile _bsp;
        private readonly FileStream _stream;
        private readonly BinaryReader _reader;

        public BspFileReader(BspFile file)
        {
            _bsp = file;
            _stream = File.OpenRead(_bsp.FilePath);
            _reader = new BinaryReader(_stream);
        }

        public void Load()
        {
            ReadHeader();
            LoadAll();
            ResolveTexNames();
            ResolveTexData();
        }

        private void ReadHeader()
        {
            if (_stream.Position != 0) _stream.Seek(0, 0);

            var ident = _reader.ReadBytes(4);

            if (Encoding.Default.GetString(ident) != "VBSP") throw new InvalidDataException();

            _bsp.Version = _reader.ReadInt32();

            for (var i = 0; i < BspFile.HeaderLumps; i++)
            {
                var type = (LumpType)i;

                Lump lump = type switch
                {
                    LumpType.LUMP_ENTITIES => new EntityLump(_bsp),
                    LumpType.LUMP_TEXINFO => new TexInfoLump(_bsp),
                    LumpType.LUMP_TEXDATA => new TexDataLump(_bsp),
                    LumpType.LUMP_TEXDATA_STRING_TABLE => new TexDataStringTableLump(_bsp),
                    LumpType.LUMP_TEXDATA_STRING_DATA => new TexDataStringDataLump(_bsp),
                    _ => new UnmanagedLump(_bsp)
                };

                lump.Type = type;
                lump.Offset = _reader.ReadInt32();
                lump.Length = _reader.ReadInt32();
                lump.Version = _reader.ReadInt32();
                lump.FourCc = _reader.ReadInt32();

                Console.WriteLine($"Lump {lump.Type}({(int)lump.Type})\toffset: {lump.Offset}\t length: {lump.Length}");

                _bsp.Lumps.Add(lump);
            }

            _bsp.Revision = _reader.ReadInt32();
        }

        private void LoadAll()
        {
            foreach (var l in _bsp.Lumps.Where(lump => lump is ManagedLump))
            {
                var lump = (ManagedLump)l;
                LoadLump(lump);
            }
        }

        private void LoadLump(ManagedLump lump)
        {
            if (lump.Length == 0) return;

            _stream.Seek(lump.Offset, 0);

            lump.Read(_reader);
        }

        public void CopyLumpStream(Lump lump, Stream output)
        {
            if (lump.Length > 0)
            {
                _stream.Seek(lump.Offset, SeekOrigin.Begin);

                var read = 0;
                var bytes = lump.Length;
                var buffer = new byte[64 * 1024];

                while ((read = _stream.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
                {
                    output.Write(buffer, 0, read);
                    bytes -= read;
                }
            }
        }

        // ARGH NO NO NO
        public FileStream GetLumpStream(Lump lump)
        {
            _stream.Seek(lump.Offset, 0);

            return _stream;
        }

        private void ResolveTexNames()
        {
            var texDataLump = _bsp.GetLump<TexDataLump>();
            foreach (var texture in texDataLump.Data)
            {
                var name = new StringBuilder();
                var texDataStringTableLump = _bsp.GetLump<TexDataStringTableLump>();
                var stringtableoffset = texDataStringTableLump.Data[texture.StringTablePointer];
                var texDataStringDataLump = _bsp.GetLump<TexDataStringDataLump>();

                //todo checks
                var end = Array.FindIndex(texDataStringDataLump.Data, stringtableoffset, (x => x == 0));
                texture.TexName = Encoding.ASCII.GetString(texDataStringDataLump.Data, stringtableoffset, end - stringtableoffset);
            }
        }

        private void ResolveTexData()
        {
            foreach (var texinfo in _bsp.GetLump<TexInfoLump>().Data)
            {
                texinfo.TexData = _bsp.GetLump<TexDataLump>().Data[texinfo.TexDataPointer];
            }
        }

        public void Dispose() => _reader.Dispose();
    }
}
