namespace Lumper.Lib.BSP.Struct;
using Lumper.Lib.BSP.Enum;

public class TexInfo
{
    public float[,] TextureVectors { get; set; }
    public float[,] LightmapVectors { get; set; }
    public SurfaceFlag Flags { get; set; }
    public int TexDataPointer { get; set; }

    // TODO: texdata values a texinfo can take currently limited to existing items in texdata array.
    // if texinfo gets assigned a texdata instance not in the texdatalump data it won't get written.
    public TexData TexData { get; set; }
}
