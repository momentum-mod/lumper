using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MomBspTools.Lib.BSP
{
    public class BspFile
    {
        private const int HeaderLumps = 64;
        private const int HeaderSize = 1036;
        private const int MaxLumps = 128;

        private string _file;
        private string _ident;
        private string _name;
        private int _revision;
        private int _version;

        List<Lump> lumps = new(HeaderLumps);

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

            for (int i = 0; i < HeaderLumps; i++)
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
                
                if (ofs == 0 && len == 0)
                {
                    Console.WriteLine($"{Enum.GetName(type),-36} {new string('-', 31)} NO DATA {new string('-', 31)}");
                }
                else
                {
                    Console.WriteLine("{0,-36} Offset {1,-10} Length {2,-10} Version {3,-10} FourCC {4}", Enum.GetName(type), ofs, len, vers, fourcc);
                }
            }
        }

        public List<Lump> GetLumps() { return lumps; }
        
        public Lump GetLump(LumpType lumpType)
        {
            return lumps.FirstOrDefault(x => x.Type == lumpType);
        }
    }
}