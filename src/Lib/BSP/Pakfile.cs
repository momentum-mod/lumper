using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using MomBspTools.Lib.BSP.Lumps;

namespace MomBspTools.Lib.BSP
{
    public class Pakfile
    {
        public BspFile ParentFile { get; }
        
        public Lump Paklump { get; }

        public Pakfile(BspFile bspFile)
        {
            ParentFile = bspFile;
            Paklump = bspFile.GetLump(LumpType.LUMP_PAKFILE);
        }
        // TODO: kill DX80s/SWs?
        public ZipArchive GetZipArchive()
        {
            return new ZipArchive(ParentFile.GetLumpStream(Paklump));
        }
    }
}