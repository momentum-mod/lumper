namespace Lumper.Lib.Bsp.Lumps.BspLumps;

using System.IO;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.Lumps;

public class TexDataStringTableLump(BspFile parent) : FixedLump<BspLumpType, int>(parent)
{
    public override int StructureSize => 4;

    protected override void ReadItem(BinaryReader reader) => Data.Add(reader.ReadInt32());

    protected override void WriteItem(BinaryWriter writer, int index) => writer.Write(Data[index]);
}
