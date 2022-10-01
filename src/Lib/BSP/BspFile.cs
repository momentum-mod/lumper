using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.IO;

namespace Lumper.Lib.BSP
{
    public class BspFile
    {
        public const int HeaderLumps = 64;
        public const int HeaderSize = 1036;
        public const int MaxLumps = 128;

        public string FilePath { get; private set; }
        public string Name { get; private set; }
        public int Revision { get; set; }
        public int Version { get; set; }

        public BspFileReader reader;

        public Dictionary<BspLumpType, Lump<BspLumpType>> Lumps { get; set; } = new();

        public BspFile()
        { }

        public BspFile(string path)
        {
            Load(path);
        }

        public void Load(string path)
        {
            // TODO: loads of error handling
            Name = Path.GetFileNameWithoutExtension(path);
            FilePath = Path.GetFullPath(path);

            if (!File.Exists(FilePath))
                throw new FileNotFoundException();
            var stream = File.OpenRead(FilePath);
            reader = new BspFileReader(this, stream);
            reader.Load();
        }

        public void Save(string path)
        {
            // if (File.Exists(path)) Console.WriteLine("File already exists!");
            // else
            // {
            File.WriteAllText(path, null);
            using var writer = new BspFileWriter(this, File.OpenWrite(path));
            writer.Save();
            // }
        }

        public T GetLump<T>() where T : Lump<BspLumpType>
        {
            // if you add something here, also add it to the BspReader
            Dictionary<System.Type, BspLumpType> typeMap = new()
            {
                { typeof(EntityLump), BspLumpType.Entities },
                { typeof(TexInfoLump), BspLumpType.Texinfo },
                { typeof(TexDataLump), BspLumpType.Texdata },
                { typeof(TexDataStringTableLump), BspLumpType.Texdata_string_table },
                { typeof(TexDataStringDataLump), BspLumpType.Texdata_string_data },
                { typeof(PakFileLump), BspLumpType.Pakfile },
                { typeof(GameLump), BspLumpType.Game_lump }
            };

            if (typeMap.ContainsKey(typeof(T)))
            {
                return (T)Lumps[typeMap[typeof(T)]];
            }
            var tLumps = Lumps.Where(x => x.Value.GetType() == typeof(T));
            return (T)tLumps.Select(x => x.Value).First();
        }

        public Lump<BspLumpType> GetLump(BspLumpType lumpType)
        {
            return Lumps[lumpType];
        }
    }
}