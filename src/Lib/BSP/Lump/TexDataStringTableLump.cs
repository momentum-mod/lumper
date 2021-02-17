using System.Collections.Generic;
using System.IO;

namespace MomBspTools.Lib.BSP.Lump
{
    public class TexDataStringTableLump : AbstractLump
    {
        public List<int> Data { get; set; } = new();

        public override int DataSize => 4;

        public override void Read(BinaryReader r)
        {
            Data.Add(r.ReadInt32());
        }

        public TexDataStringTableLump(BspFile parent) : base(parent)
        {
        }
    }
}