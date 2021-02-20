using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexDataStringDataLump : ManagedLump
    {
        public byte[] Data;
        
        public override void Read(BinaryReader r)
        {
            Data = r.ReadBytes(Length);
        }

        public override void Write(BinaryWriter r)
        {
            throw new System.NotImplementedException();
        }

        public TexDataStringDataLump(BspFile parent) : base(parent)
        {
        }
    }
}