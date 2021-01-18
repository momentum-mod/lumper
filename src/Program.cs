using System;
using System.IO;
using MomBspTools.Lib.BSP;
using MomBspTools.Lib.VTF;

namespace MomBspTools
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
                Console.WriteLine("ERROR: No arguments were given.");
                return;
            }
            //
            // try
            // {
            //     VtfFile vtf = new VtfFile(args[0]);
            // } 
            // catch (ArgumentException e)
            // {
            //     Console.WriteLine(e.Message);
            // }

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
            catch (FileNotFoundException e)
            {
                Console.WriteLine("File {0} not found!", path);
            }
        }

        static void Usage()
        {
            // TODO
        }
    }
}
