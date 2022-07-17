using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Lumps;

namespace MomBspTools.Lib.BSP
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

        // TODO: constructor w/ str that calls load()

        // Keep a main reader open for now, maybe change down the line idk
        public BspFileReader reader;

        public List<Lump> Lumps { get; set; } = new(HeaderLumps);

        public void Load(string path)
        {
            // TODO: loads of error handling
            Name = Path.GetFileNameWithoutExtension(path);
            FilePath = Path.GetFullPath(path);

            if (!File.Exists(FilePath)) throw new FileNotFoundException();

            reader = new BspFileReader(this);
            reader.Load();
        }

        public void Save(string path)
        {
            // if (File.Exists(path)) Console.WriteLine("File already exists!");
            // else
            // {
            File.WriteAllText(path, null);
            using var writer = new BspFileWriter(this, path);
            writer.Save();
            // }
        }

        // this feels fucking insane but it works
        public T GetLump<T>() => (T)(object)Lumps.First(x => x.GetType() == typeof(T));

        public Lump GetLump(LumpType lumpType) => Lumps.First(x => x.Type == lumpType);

        public FileStream GetLumpStream(Lump lump) => new BspFileReader(this).GetLumpStream(lump);
    }
}