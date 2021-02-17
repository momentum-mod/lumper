using System.Collections.Generic;
using System.IO;
using MomBspTools.Lib.BSP.Enum;

namespace MomBspTools.Lib.BSP.Lump
{
    public class TexInfoLump : AbstractLump
    {
        public override int DataSize => 72;

        public struct TexInfo
        {
            public float[,] TextureVectors { get; set; }
            public float[,] LightmapVectors { get; set; }
            public SurfaceFlag Flags { get; set; }
            public int TexDataPointer { get; set; }
            public TexDataLump.TexData TexData { get; set; }
        }

        public List<TexInfo> Data { get; set; } = new();

        public override void Read(BinaryReader r)
        {
            var item = new TexInfo
            {
                TextureVectors = new float[2, 4]
                {
                    {r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()},
                    {r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()}
                },
                LightmapVectors = new float[2, 4]
                {
                    {r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()},
                    {r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()}
                },
                Flags = (SurfaceFlag) r.ReadInt32(),
                TexDataPointer = r.ReadInt32()
            };
            Data.Add(item);
        }

        public TexInfoLump(BspFile parent) : base(parent)
        {
        }
    }
}