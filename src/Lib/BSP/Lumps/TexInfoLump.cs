using System.Collections.Generic;
using System.IO;
using MomBspTools.Lib.BSP.Enum;
using MomBspTools.Lib.BSP.Struct;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexInfoLump : FixedLump
    {
        protected override int Size => 72;

        public List<TexInfo> Data { get; set; } = new();

        protected override void ReadItem(BinaryReader r)
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

        public override void Write(BinaryWriter r)
        {
            throw new System.NotImplementedException();
        }

        public TexInfoLump(BspFile parent) : base(parent)
        {
        }
    }
}