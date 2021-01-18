using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MomBspTools
{
    public class BspFile
    {
        private const int HEADER_LUMPS = 64;
        private const int HEADER_SIZE = 1036;
        private const int MAX_LUMPS = 128;

        private string _file;
        private string _ident;
        private string _name;
        private int _revision;
        private int _version;

        List<Lump> lumps = new List<Lump>(HEADER_LUMPS);

        public BspFile()
        {
            
        }

        public BspFile(string path)
        {
            Load(path);
        }

        public void Load(string path)
        {
            _file = Path.GetFullPath(path);
            _name = Path.GetFileNameWithoutExtension(path);

            BinaryReader reader = new BinaryReader(File.OpenRead(_file));

            _ident = Encoding.Default.GetString(reader.ReadBytes(4));
            _version = reader.ReadInt32();

            Console.WriteLine("Reading BSP {0} with header ident {1} and version {2}", _file, _ident, _version);

            for (int i = 0; i < HEADER_LUMPS; i++)
            {
                int ofs = reader.ReadInt32();
                int len = reader.ReadInt32();
                int vers = reader.ReadInt32();
                int fourcc = reader.ReadInt32();
                LumpType type = (LumpType) i;

                Lump l = new Lump
                {
                    Index = i,
                    Type = type,
                    Offset = ofs,
                    Length = len,
                    Version = vers,
                    FourCC = fourcc
                };
                lumps.Add(l);
                Console.WriteLine("Reading {0}, offset {1}, length {2}, version {3}, fourcc {4}", Enum.GetName(type), ofs, len, vers, fourcc);
            }
        }

        public List<Lump> GetLumps() { return lumps; }
        
        public Lump GetLump(LumpType lumpType)
        {
            return lumps.FirstOrDefault(x => x.Type == lumpType);
        }
    }
}