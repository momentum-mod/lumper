using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexDataStringDataLump : ManagedLump
    {
        public byte[] Data;

        public override void Read(BinaryReader reader)
        {
            Data = reader.ReadBytes(Length);
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Data);
        }

        public TexDataStringDataLump(BspFile parent) : base(parent)
        {
        }
    }
}