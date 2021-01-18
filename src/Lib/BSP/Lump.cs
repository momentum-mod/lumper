namespace MomBspTools.Lib.BSP
{
    public class Lump
    {
        private int _index;
        
        public Lump()
        {

        }

        public int Index { get; set; }
        public LumpType Type { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public int Version { get; set; }
        public int FourCC {  get; set; }
    }
}