namespace Lumper.Lib.BSP.IO
{
    public class GameLumpHeader
    {
        public GameLumpHeader()
        { }

        public GameLumpHeader(LumpHeader tmpHeader, ushort version, int id)
        {
            Id = id;
            FileOfs = (int)tmpHeader.Offset;
            //set last bit to 0 (only seen flags as 1 and 0 so far but its probably a bitfield)
            Flags &= 0xFFFE;
            //length is always the uncompressed length
            FileLen = (int)tmpHeader.UncompressedLength;
            if (tmpHeader.Compressed)
            {
                //set flag if its compressed
                Flags += 1;
            }
            Version = version;
        }
        public const int StructureSize = 16;
        public int Id { get; set; }
        public ushort Flags { get; set; }
        public ushort Version { get; set; }
        public int FileOfs { get; set; }
        public int FileLen { get; set; }

    }
}