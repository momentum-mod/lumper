using System.Collections.Generic;
using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexDataStringTableLump : FixedLump
    {
        public List<int> Data { get; set; } = new();

        protected override int Size => 4;

        protected override void ReadItem(BinaryReader r)
        {
            Data.Add(r.ReadInt32());
        }

        public override void Write(BinaryWriter r)
        {
            throw new System.NotImplementedException();
        }

        public TexDataStringTableLump(BspFile parent) : base(parent)
        {
        }
    }
}