using System;
using System.IO;
using System.Text;
using Lumper.Lib.BSP.Struct;

namespace Lumper.Lib.BSP.Lumps.GameLumps
{
    public class StaticPropLeafLump : FixedLump<GameLumpType, uint>
    {
        public override int StructureSize => (Version == 12)
                                                ? 4
                                                : 2;

        protected override void ReadItem(BinaryReader reader)
        {
            if (Version == 12)
                Data.Add(reader.ReadUInt32());
            else
                Data.Add(reader.ReadUInt16());
        }
        protected override void WriteItem(BinaryWriter writer, int index)
        {
            if (Version == 12)
                writer.Write(Data[index]);
            else
                writer.Write((ushort)Data[index]);
        }

        public StaticPropLeafLump(BspFile parent) : base(parent)
        {
        }
    }
}