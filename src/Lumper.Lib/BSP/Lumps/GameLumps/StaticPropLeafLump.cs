namespace Lumper.Lib.Bsp.Lumps.GameLumps;

using System.IO;
using Lumper.Lib.Bsp.Enum;

public class StaticPropLeafLump(BspFile parent) : FixedLump<GameLumpType, uint>(parent)
{
    public override int StructureSize => Version == 12 ? 4 : 2;

    protected override void ReadItem(BinaryReader reader)
    {
        if (Version >= 12)
            Data.Add(reader.ReadUInt32());
        else
            Data.Add(reader.ReadUInt16());
    }

    protected override void WriteItem(BinaryWriter writer, int index)
    {
        if (Version >= 12)
            writer.Write(Data[index]);
        else
            writer.Write((ushort)Data[index]);
    }
}
