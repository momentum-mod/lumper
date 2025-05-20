namespace Lumper.Lib.Bsp.Lumps.GameLumps;

using System;
using System.IO;
using Lumper.Lib.Bsp.Enum;
using NLog;

public class StaticPropDictLump(BspFile parent) : FixedLump<GameLumpType, string>(parent)
{
    public override int StructureSize => 128;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override void ReadItem(BinaryReader reader)
    {
        Data.Add(new string(reader.ReadChars(StructureSize)).TrimEnd('\0'));
    }

    protected override void WriteItem(BinaryWriter writer, int index)
    {
        byte[] bytes = new byte[StructureSize];
        byte[] value = BspFile.Encoding.GetBytes(Data[index]);

        int count = value.Length;
        if (count > StructureSize)
        {
            Logger.Warn($"{GetType().Name} string to long!");
            count = StructureSize;
        }

        Array.Copy(value, bytes, count);
        writer.Write(bytes);
    }
}
