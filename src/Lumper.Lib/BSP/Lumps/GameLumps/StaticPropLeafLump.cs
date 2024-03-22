namespace Lumper.Lib.BSP.Lumps.GameLumps;
using System.IO;

public class StaticPropLeafLump(BspFile parent) : FixedLump<GameLumpType, uint>(parent)
{
    public override int StructureSize => Version == 12 ? 4 : 2;

    protected override void ReadItem(BinaryReader reader) =>
        Data.Add(Version == 12
            ? reader.ReadUInt32()
            : reader.ReadUInt16());

    protected override void WriteItem(BinaryWriter writer, int index) =>
        writer.Write(Version == 12
            ? Data[index]
            : (ushort)Data[index]);
}
