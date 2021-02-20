using System.Collections.Generic;
using System.IO;
using MomBspTools.Lib.BSP.Struct;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexDataLump : FixedLump
    {
        protected override int Size => 32;
        
        public List<TexData> Data { get; set; } = new();

        protected override void ReadItem(BinaryReader r)
        {
            var item = new TexData
            {
                Reflectivity = new float[3] {r.ReadSingle(), r.ReadSingle(), r.ReadSingle()},
                TexName = r.ReadInt32(),
                Width = r.ReadInt32(),
                Height = r.ReadInt32(),
                ViewWidth = r.ReadInt32(),
                ViewHeight = r.ReadInt32()
            };
            Data.Add(item);
        }

        public override void Write(BinaryWriter r)
        {
            throw new System.NotImplementedException();
        }

        public TexDataLump(BspFile parent) : base(parent)
        {
        }
    }
}