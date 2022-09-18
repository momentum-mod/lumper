using System;
using System.IO;
using System.Text;
using Lumper.Lib.BSP.Struct;

namespace Lumper.Lib.BSP.Lumps.GameLumps
{
    public class StaticPropLeafLump : FixedLump<GameLumpType, ushort>
    {
        public override int StructureSize => 2;

        protected override void ReadItem(BinaryReader reader)
        {
            Data.Add(reader.ReadUInt16());
        }
        protected override void WriteItem(BinaryWriter writer, int index)
        {
            writer.Write(Data[index]);
        }

        public StaticPropLeafLump(BspFile parent) : base(parent)
        {
        }
    }
}