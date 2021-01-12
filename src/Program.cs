using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace MomBspTools
{
    class Program
    {
        static void Main(string[] args)
        {
            const string _fileName = "../../../surf_lt_omnific.bsp";
            
            if (File.Exists(_fileName))
            {
                BspFile map = new BspFile(_fileName);
                Lump pakFile = map.GetLump(LumpType.LUMP_PAKFILE);
            }
        }
    }
}
