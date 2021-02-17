using MomBspTools.Lib.BSP.Enum;

namespace MomBspTools.Lib.BSP.Lump
{
    public class TexInfo
    {
        public float[,] TextureVectors { get; set; }
        public float[,] LightmapVectors { get; set; }
        public SurfaceFlag Flags { get; set; }
        public int TexDataPointer { get; set; }
        public TexData TexData { get; set; }
    }
}