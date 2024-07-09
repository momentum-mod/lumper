namespace Lumper.Lib.BSP.Lumps.GameLumps;

using System;
using System.IO;
using System.Text;
using Enum;
using NLog;

public class StaticPropDictLump(BspFile parent) : FixedLump<GameLumpType, string>(parent)
{
    public override int StructureSize => 128;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override void ReadItem(BinaryReader reader)
        => Data.Add(new string(reader.ReadChars(StructureSize)));

    protected override void WriteItem(BinaryWriter writer, int index)
    {
        var b = new byte[StructureSize];
        var value = Encoding.ASCII.GetBytes(Data[index]);
        var count = value.Length;
        if (count > StructureSize)
        {
            Logger.Warn($"{GetType().Name} string to long!");
            count = StructureSize;
        }

        Array.Copy(value, b, count);
        writer.Write(b);
    }
}
