using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MomBspTools.Lib.BSP;

namespace MomBspTools
{
    static class Program
    {
        public static void Main(string[] args)
        {
            
            if (args.Length < 1)
            {
                Usage();
                Console.WriteLine("ERROR: No arguments were given.");
                return;
            }
            
            foreach (var fileName in args)
            {
                LoadMap(fileName);
            }
        }

        private static void LoadMap(string path)
        {
            try
            {
                var map = new BspFile();
                map.Load(path);
                foreach (var ent in map.EntityLump.Data.FindAll(entity => entity.ClassName.Contains("logic")))
                {
                    Console.WriteLine("Ent {0}: index {1}", ent.ClassName, map.EntityLump.Data.FindIndex(entity => entity == ent));
                }
                // var pakfile = new Pakfile(map);
                // ZipArchive archive = pakfile.GetZipArchive();
                // foreach (var entry in archive.Entries)
                // {
                //     Console.WriteLine(entry.Name);
                // }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("ERROR: File {0} not found.", path);
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("ERROR: File {0} is not a valid Valve BSP.", path);
            }
        }

        private static void Usage()
        {
            // TODO
        }
    }
}
