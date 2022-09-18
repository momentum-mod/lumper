using System.IO;
using System.Text;

namespace Lumper.Lib.BSP.Lumps.BspLumps
{
    public class TexDataStringDataLump : ManagedLump<BspLumpType>
    {
        public static readonly Encoding TextureNameEncoding = Encoding.UTF8;
        public byte[] Data;

        public override void Read(BinaryReader reader, long length)
        {
            Data = reader.ReadBytes((int)length);
        }

        public override void Write(Stream stream)
        {
            stream.Write(Data, 0, Data.Length);
        }

        public override bool Empty()
        {
            return Data.Length <= 0;
        }

        public TexDataStringDataLump(BspFile parent) : base(parent)
        {
        }
    }
}