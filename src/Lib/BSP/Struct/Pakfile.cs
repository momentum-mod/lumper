using System.IO;
using System.IO.Compression;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Lumps;

namespace MomBspTools.Lib.BSP.Struct
{
    public class Pakfile
    {
        public BspFile ParentFile { get; }

        public PakFileLump Paklump { get; }
        private ZipArchive Zip { get; set; }

        public Pakfile(BspFile bspFile)
        {
            ParentFile = bspFile;
            Paklump = bspFile.GetLump<PakFileLump>();
            Zip = new(Paklump.Data, ZipArchiveMode.Update);
        }
        // TODO: kill DX80s/SWs?
        public ZipArchive GetZipArchive()
        {
            return Zip;
        }
    }
}