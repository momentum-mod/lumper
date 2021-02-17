using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexDataStringDataLump : Lump
    {
        public byte[] Data;
        
        public override int DataSize => 1;
        
        public override void Read(BinaryReader r)
        {
            Data = r.ReadBytes(Length);
        }
        
        public TexDataStringDataLump(BspFile parent) : base(parent)
        {
        }
    }
}