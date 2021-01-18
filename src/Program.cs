using System;
using System.IO;
using MomBspTools.Lib.BSP;

namespace MomBspTools
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No arguments were given.");
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
                BspFile map = new BspFile(path);
                Lump pakFile = map.GetLump(LumpType.LUMP_PAKFILE);
            }
            catch (FileNotFoundException e) {
                
            }
        }
    }
}
