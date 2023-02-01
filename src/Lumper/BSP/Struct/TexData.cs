namespace Lumper.Lib.BSP.Struct
{
    public class TexData
    {
        public float[] Reflectivity { get; set; }
        public string TexName { get; set; }
        public int StringTablePointer { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ViewWidth { get; set; }
        public int ViewHeight { get; set; }
    }
}