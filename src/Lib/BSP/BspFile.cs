using System.Collections.Generic;
using System.IO;
using System.Linq;
using MomBspTools.Lib.BSP.Lumps;

namespace MomBspTools.Lib.BSP
{
    public class BspFile
    {
        public string FilePath { get; private set; }
        public string Name { get; private set; }
        public int Revision { get; set; }
        public int Version { get; set; }

        public EntityLump EntityLump { get; set; }
        public TexDataLump TexDataLump { get; set; }
        public TexDataLump TexStringDataLump { get; set; }
        
        public List<Lump> Lumps { get; set; } = new(HeaderLumps);

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
        }

        public Lump GetLump(LumpType lumpType)
        {
            return Lumps.First(x => x.Type == lumpType);
        }

        public MemoryStream GetLumpStream(Lump lump)
        {
             var reader = new BspFileReader(this);
            return reader.GetLumpStream(lump);
        }
    }
}