namespace Lumper.Lib.BSP.IO;

public class BspLumpHeader(LumpHeader header, int version)
{
    public int Offset { get; set; } = (int)header.Offset;
    public int Length { get; set; } = (int)header.Length;
    public int Version { get; set; } = version;
    public int FourCc { get; set; } = header.Compressed ? (int)header.UncompressedLength : 0;
}
