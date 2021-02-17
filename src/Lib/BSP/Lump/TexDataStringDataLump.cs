using System.IO;

namespace MomBspTools.Lib.BSP.Lump
{
    public class TexDataStringDataLump : AbstractLump
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