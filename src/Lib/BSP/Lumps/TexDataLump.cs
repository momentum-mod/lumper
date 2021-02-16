using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class TexDataLump : Lump
    {
        public override int DataSize => 32;
        
        public struct TexData
        {
            public Vector3 Reflectivity { get; set; }
            public int TexName { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int ViewWidth { get; set; }
            public int ViewHeight { get; set; }
        }

        public List<TexData> Data { get; set; } = new();
        
        public override void Read(BinaryReader reader)
        {
            var item = new TexData
            {
                Reflectivity = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                TexName = reader.ReadInt32(),
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
                ViewWidth = reader.ReadInt32(),
                ViewHeight = reader.ReadInt32()
            };
            Data.Add(item);
        }
    }
}