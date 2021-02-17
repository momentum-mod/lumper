using System.IO.Compression;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Lump;

namespace MomBspTools.Lib.BSP
{
    public class Pakfile
    {
        public BspFile ParentFile { get; }
        
        public GenericLump Paklump { get; }

        public Pakfile(BspFile bspFile)
        {
            ParentFile = bspFile;
            Paklump = (GenericLump) bspFile.GetLump(LumpType.LUMP_PAKFILE);
        }
        // TODO: kill DX80s/SWs?
        public ZipArchive GetZipArchive()
        {
            return new ZipArchive(ParentFile.GetLumpStream(Paklump));
        }
    }
}