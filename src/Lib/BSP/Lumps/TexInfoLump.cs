using System.Collections.Generic;
using System.IO;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Struct;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexInfoLump : FixedLump<TexInfo>
    {
        protected override int StructureSize => 72;

        protected override void ReadItem(BinaryReader reader)
        {
            var item = new TexInfo
            {
                TextureVectors = new float[2, 4],
                LightmapVectors = new float[2, 4]
            };

            for (var i = 0; i < 2; i++)
            for (var j = 0; j < 4; j++)
                item.TextureVectors[i, j] = reader.ReadSingle();

            for (var i = 0; i < 2; i++)
            for (var j = 0; j < 4; j++)
                item.LightmapVectors[i, j] = reader.ReadSingle();

            item.Flags = (SurfaceFlag) reader.ReadInt32();
            item.TexDataPointer = reader.ReadInt32();

            Data.Add(item);
        }

        protected override void WriteItem(BinaryWriter writer, int index)
        {
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 4; j++)
                    writer.Write(Data[index].TextureVectors[i, j]);
            }

            for (var i = 0; i < 2; i++)
            for (var j = 0; j < 4; j++)
                writer.Write(Data[index].LightmapVectors[i, j]);

            writer.Write((int) Data[index].Flags);
            writer.Write((Data[index].TexDataPointer));
        }

        public TexInfoLump(BspFile parent) : base(parent)
        {
        }
    }
}