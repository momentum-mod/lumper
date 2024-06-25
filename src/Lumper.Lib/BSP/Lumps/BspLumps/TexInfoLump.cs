namespace Lumper.Lib.BSP.Lumps.BspLumps;
using System.IO;
using Lumper.Lib.BSP.Enum;
using Lumper.Lib.BSP.Struct;

public class TexInfoLump(BspFile parent) : FixedLump<BspLumpType, TexInfo>(parent)
{
    public override int StructureSize => 72;

    protected override void ReadItem(BinaryReader reader)
    {
        var item = new TexInfo
        {
            TextureVectors = new float[2, 4],
            LightmapVectors = new float[2, 4]
        };

        for (var i = 0; i < 2; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                item.TextureVectors[i, j] = reader.ReadSingle();
            }
        }

        for (var i = 0; i < 2; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                item.LightmapVectors[i, j] = reader.ReadSingle();
            }
        }

        item.Flags = (SurfaceFlag)reader.ReadInt32();
        item.TexDataPointer = reader.ReadInt32();

        Data.Add(item);
    }

    protected override void WriteItem(BinaryWriter writer, int index)
    {
        TexInfo texInfo = Data[index];
        for (var i = 0; i < 2; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                writer.Write(texInfo.TextureVectors[i, j]);
            }
        }

        for (var i = 0; i < 2; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                writer.Write(texInfo.LightmapVectors[i, j]);
            }
        }

        writer.Write((int)texInfo.Flags);
        writer.Write(texInfo.TexDataPointer);
    }
}
