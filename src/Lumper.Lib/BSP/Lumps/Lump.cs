namespace Lumper.Lib.BSP.Lumps;

using System.IO;
using Enum;
using JsonSubTypes;
using Newtonsoft.Json;

// Lump with an enum type variable
public abstract class Lump<T>(BspFile parent) : Lump(parent) where T : System.Enum
{
    public T Type { get; set; } = default!;
}

// Implements how a lump will be read from a stream (same for write).
// Doesn't store offset or length because header information is separate.
[JsonConverter(typeof(JsonSubtypes), "Class")]
public abstract class Lump(BspFile parent)
{
    public string Class => GetType().Name;
    public bool Compress { get; set; }
    [JsonIgnore]
    public BspFile Parent { get; set; } = parent;

    public int Version { get; set; }

    public int Flags { get; set; }


    public abstract void Read(BinaryReader reader, long length);

    public abstract void Write(Stream stream);

    public abstract bool Empty { get; }
}
