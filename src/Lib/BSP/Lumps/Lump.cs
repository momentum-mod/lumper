using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public abstract class Lump
    {
        public LumpType Type { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public int Version { get; set; }
        public int FourCc { get; set; }

        public abstract int DataSize { get; }
        public abstract void Read(BinaryReader reader);
    }
}