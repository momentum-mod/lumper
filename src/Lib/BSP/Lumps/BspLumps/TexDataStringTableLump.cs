using System.Collections.Generic;
using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexDataStringTableLump : FixedLump<int>
    {
        protected override int StructureSize => 4;

        protected override void ReadItem(BinaryReader reader)
        {
            Data.Add(reader.ReadInt32());
        }
        protected override void WriteItem(BinaryWriter writer, int index)
        {
            writer.Write(Data[index]);
        }

        public TexDataStringTableLump(BspFile parent) : base(parent)
        {
        }
    }
}