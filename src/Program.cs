using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MomBspTools.Lib.BSP;
using MomBspTools.Lib.BSP.Lumps;

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

            var map1 = new BspFile();
            map1.Load(args[0]);
            //LoadMap(args[0]);
            
             foreach (var texture in map1.GetLump<TexDataLump>().Data)
             {
                 texture.TexName = "R997/Mc/Mc-Jackolantern";
             }
            map1.Save("test.bsp");

             var map2 = new BspFile(); 
             map2.Load("test.bsp");

            //
            // foreach (var ent in map.GetLump<EntityLump>().Data.Where(ent => ent.Properties.ContainsKey("skyname")))
            // {
            //     ent.Properties["skyname"] = "sky_day01_09";
            //     Console.WriteLine("hi");
            // }

            // var lastoffset = 0;
            // foreach (var lump in map.Lumps.OrderBy(lump => lump.Offset))
            // {
            //     Console.WriteLine("Lump {0}, offset {1}, length {2}, last offset {3}, diff {4}", lump.Type, lump.Offset, lump.Length, lastoffset, lump.Offset - lastoffset);
            //     lastoffset = lump.Offset;
            // }

            // var pakfile = new Pakfile(map);
            // ZipArchive archive = pakfile.GetZipArchive();
            // foreach (var entry in archive.Entries)
            // {
            //     Console.WriteLine(entry.Name);
            // }

        }

        private static BspFile LoadMap(string path)
        {
            try
            {
                var map = new BspFile();
                
                map.Load(path);

                return map;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("ERROR: File {0} not found.", path);
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("ERROR: File {0} is not a valid Valve BSP.", path);
            }

            return null;
        }

        private static void Usage()
        {
            // TODO
        }
    }
}
