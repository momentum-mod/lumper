namespace Lumper.Lib.BSP.IO;

public class BspLumpHeader(LumpHeaderInfo headerInfo, int version)
{
    public int Offset { get; set; } = (int)headerInfo.Offset;
    public int Length { get; set; } = (int)headerInfo.Length;
    public int Version { get; set; } = version;
    public int FourCc { get; set; } = headerInfo.Compressed ? (int)headerInfo.UncompressedLength : 0;
}
