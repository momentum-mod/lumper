using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Lump;

namespace MomBspTools.Lib.BSP
{
    public class BspFile
    {
        public string FilePath { get; private set; }
        public string Name { get; private set; }
        public int Revision { get; set; }
        public int Version { get; set; }

        public EntityLump EntityLump { get; set; }
        public TexInfoLump TexInfoLump { get; set; }
        public TexDataLump TexDataLump { get; set; }
        public TexDataStringTableLump TexDataStringTableLump { get; set; }
        public TexDataStringDataLump TexDataStringDataLump { get; set; }
        
        public List<AbstractLump> Lumps { get; set; } = new(HeaderLumps);

        public const int HeaderLumps = 64;
        public const int HeaderSize = 1036;
        public const int MaxLumps = 128;

        public void Load(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            FilePath = Path.GetFullPath(path);

            if (!File.Exists(FilePath)) throw new FileNotFoundException();

            var reader = new BspFileReader(this);
            reader.LoadHeader();
            reader.LoadAllLumps();

            TexDataLump = (TexDataLump) GetLump(LumpType.LUMP_TEXDATA);
            TexInfoLump = (TexInfoLump) GetLump(LumpType.LUMP_TEXINFO);
            TexDataStringTableLump = (TexDataStringTableLump) GetLump(LumpType.LUMP_TEXDATA_STRING_TABLE);
            TexDataStringDataLump = (TexDataStringDataLump) GetLump(LumpType.LUMP_TEXDATA_STRING_DATA);
            
            ResolveTexNames();
            ResolveTexData();
        }

        private void ResolveTexData()
        {
            foreach (var texinfo in TexInfoLump.Data)
            {
                texinfo.TexData = TexDataLump.Data[texinfo.TexDataPointer];
            }
        }

        private void ResolveTexNames()
        {
            foreach (var texture in TexDataLump.Data)
            {
                var stringtableoffset = TexDataStringTableLump.Data[texture.TexName];
                var name = "";
                char nextchar;
                do
                {
                    nextchar = Convert.ToChar(TexDataStringDataLump.Data[stringtableoffset]);
                    name += nextchar;
                    stringtableoffset++;
                } while (nextchar != '\0');
                
                texture.TexNameString = name;
            }
        }

        public AbstractLump GetLump(LumpType lumpType)
        {
            return Lumps.First(x => x.Type == lumpType);
        }

        public MemoryStream GetLumpStream(AbstractLump lump)
        {
            var reader = new BspFileReader(this);
            return reader.GetLumpStream(lump);
        }
    }
}