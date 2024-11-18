namespace Lumper.Lib.Bsp.Struct;

public class TexData
{
    public float[] Reflectivity { get; set; } = null!;
    public string TexName { get; set; } = null!;
    public int StringTablePointer { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int ViewWidth { get; set; }
    public int ViewHeight { get; set; }
}
