namespace Lumper.Lib.BSP.IO;

public class GameLumpHeader
{
    public GameLumpHeader()
    {
    }

    public GameLumpHeader(LumpHeaderInfo headerInfo, ushort version, int id)
    {
        Id = id;
        FileOfs = (int)headerInfo.Offset;

        // Set last bit to 0 (only seen flags as 1 and 0 so far but its probably a bitfield)
        Flags &= 0xFFFE;

        // Length is always the uncompressed length
        FileLen = (int)headerInfo.UncompressedLength;

        // Set flag if its compressed
        if (headerInfo.Compressed)
            Flags += 1;

        Version = version;
    }

    public const int StructureSize = 16;
    public int Id { get; set; }
    public ushort Flags { get; set; }
    public ushort Version { get; set; }
    public int FileOfs { get; set; }
    public int FileLen { get; set; }
}
