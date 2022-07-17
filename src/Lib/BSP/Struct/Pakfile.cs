using System.IO.Compression;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Lumps;

namespace MomBspTools.Lib.BSP.Struct
{
    public class Pakfile
    {
        public BspFile ParentFile { get; }

        public UnmanagedLump Paklump { get; }

        public Pakfile(BspFile bspFile)
        {
            ParentFile = bspFile;
            Paklump = (UnmanagedLump)bspFile.GetLump(LumpType.LUMP_PAKFILE);
        }
        // TODO: kill DX80s/SWs?
        public ZipArchive GetZipArchive()
        {
            return new(ParentFile.GetLumpStream(Paklump));
        }
    }
}