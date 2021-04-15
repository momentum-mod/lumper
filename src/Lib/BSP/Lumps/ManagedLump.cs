using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public abstract class ManagedLump : Lump
    {
        public abstract void Read(BinaryReader reader);

        public abstract void Write(BinaryWriter writer);
        
        public ManagedLump(BspFile parent) : base(parent)
        {
        }
    }
}