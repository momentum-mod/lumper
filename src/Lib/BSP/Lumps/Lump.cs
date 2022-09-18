using System.IO;

namespace Lumper.Lib.BSP.Lumps
{
    // Lump with a enum type variable 
    public abstract class Lump<T> : Lump
                          where T : System.Enum
    {
        public T Type { get; set; }
        protected Lump(BspFile parent) : base(parent)
        { }
    }

    // implements how a lump will be read from a stream (same for write)
    // doesn't store offset or length because header information is separate
    public abstract class Lump
    {
        public bool Compress { get; set; }
        public BspFile Parent { get; set; }
        public int Version { get; set; }
        public int Flags { get; set; }

        protected Lump(BspFile parent)
        {
            Parent = parent;
        }

        public abstract void Read(BinaryReader reader, long length);
        public abstract void Write(Stream stream);
        public abstract bool Empty();
    }
}