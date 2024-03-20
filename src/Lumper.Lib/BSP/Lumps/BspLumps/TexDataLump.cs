namespace Lumper.Lib.BSP.Lumps.BspLumps;
using System.IO;
using Lumper.Lib.BSP.Struct;

public class TexDataLump(BspFile parent) : FixedLump<BspLumpType, TexData>(parent)
{
    public override int StructureSize => 32;
    protected override void ReadItem(BinaryReader reader)
    {
        var item = new TexData
        {
            Reflectivity =
            [
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            ],
            StringTablePointer = reader.ReadInt32(),
            Width = reader.ReadInt32(),
            Height = reader.ReadInt32(),
            ViewWidth = reader.ReadInt32(),
            ViewHeight = reader.ReadInt32()
        };
        Data.Add(item);
    }
    protected override void WriteItem(BinaryWriter writer, int index)
    {
        writer.Write(Data[index].Reflectivity[0]);
        writer.Write(Data[index].Reflectivity[1]);
        writer.Write(Data[index].Reflectivity[2]);
        writer.Write(Data[index].StringTablePointer);
        writer.Write(Data[index].Width);
        writer.Write(Data[index].Height);
        writer.Write(Data[index].ViewWidth);
        writer.Write(Data[index].ViewHeight);
    }
}
