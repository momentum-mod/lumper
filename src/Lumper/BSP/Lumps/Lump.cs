using System.IO;
using Newtonsoft.Json;
using JsonSubTypes;

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

    [JsonConverter(typeof(JsonSubtypes), "Class")]
    public abstract class Lump
    {
        public string Class { get => this.GetType().Name; }
        public bool Compress { get; set; }
        [JsonIgnore]
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