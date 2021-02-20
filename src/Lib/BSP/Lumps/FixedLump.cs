using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    /// <summary>
    /// Lump with underlying structure of a fixed size.
    /// </summary>
    public abstract class FixedLump : ManagedLump
    {
        protected abstract int Size { get; }

        protected abstract void ReadItem(BinaryReader r);
        
        public override void Read(BinaryReader r)
        {
            // TODO: checks and shit
            var count = Length / Size;
            for (var i = 0; i < count; i++)
            {
                ReadItem(r);
            }
        }
        
        protected FixedLump(BspFile parent) : base(parent)
        {
        }
    }
}