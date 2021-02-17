using System;
using System.IO;
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

        static void LoadMap(string path)
        {
            
            try
            {
                var map = new BspFile();
                map.Load(path);
                //Console.WriteLine(System.Text.Encoding.Default.GetString(map.TexDataStringDataLump.Data));
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

        static void Usage()
        {
            // TODO
        }
    }
}
