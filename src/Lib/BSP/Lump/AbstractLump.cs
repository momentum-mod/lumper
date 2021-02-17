using System.IO;
using MomBspTools.Lib.BSP.Enum;

namespace MomBspTools.Lib.BSP.Lump
{
    public abstract class AbstractLump
    {
        public LumpType Type { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public int Version { get; set; }
        public int FourCc { get; set; }
        public BspFile Parent { get; set; }

        protected  AbstractLump(BspFile parent)
        {
            Parent = parent;
        }
        
        public abstract int DataSize { get; }
        public abstract void Read(BinaryReader r);
    }
}