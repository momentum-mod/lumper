namespace Lumper.Lib.BSP.Lumps;
using System.IO;
using JsonSubTypes;
using Newtonsoft.Json;

public abstract class Lump<T> : Lump
    where T : System.Enum
// Lump with an enum type variable
{
    public T Type { get; set; }
    protected Lump(BspFile parent) : base(parent)
    { }
}

// Implements how a lump will be read from a stream (same for write).
// Doesn't store offset or length because header information is separate.
[JsonConverter(typeof(JsonSubtypes), "Class")]
public abstract class Lump
{
    public string Class => GetType().Name;
    public bool Compress { get; set; }
    [JsonIgnore]
    public BspFile Parent { get; set; }
    public int Version { get; set; }
    public int Flags { get; set; }

    protected Lump(BspFile parent) => Parent = parent;

    public abstract void Read(BinaryReader reader, long length);
    public abstract void Write(Stream stream);
    public abstract bool Empty();
}
