using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class PakFileLump : ManagedLump
    {
        public MemoryStream Data;

        public override void Read(BinaryReader reader)
        {
            Data = new();
            reader.BaseStream.CopyTo(Data, Length);
        }

        public override void Write(BinaryWriter writer)
        {
            //Data.Seek(0, SeekOrigin.Begin);
            //Data.CopyTo(writer.BaseStream);
            writer.Write(Data.ToArray());
        }

        public PakFileLump(BspFile parent) : base(parent)
        {
        }
    }
}