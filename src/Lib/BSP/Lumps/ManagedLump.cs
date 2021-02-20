using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public abstract class ManagedLump : Lump
    {
        public ManagedLump(BspFile parent) : base(parent)
        {
        }
        
        public abstract void Read(BinaryReader r);
        public abstract void Write(BinaryWriter r);
    }
}