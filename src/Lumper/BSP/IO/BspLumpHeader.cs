namespace Lumper.Lib.BSP.IO
{
    public class BspLumpHeader
    {
        public BspLumpHeader(LumpHeader header, int version)
        {
            Offset = (int)header.Offset;
            Length = (int)header.Length;
            Version = version;
            FourCc = header.Compressed
                        ? (int)header.UncompressedLength
                        : 0;
        }

        public int Offset { get; set; }
        public int Length { get; set; }
        public int Version { get; set; }
        public int FourCc { get; set; }
    }
}