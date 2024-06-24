namespace Lumper.Lib.BSP.Lumps;

using System.IO;
using Enum;
using JsonSubTypes;
using Newtonsoft.Json;

/// <summary>
/// Lump of a specific <see cref="BspLumpType"/>
/// </summary>
public abstract class Lump<T>(BspFile parent) : Lump(parent) where T : System.Enum
{
    public T Type { get; set; } = default!;
}

/// <summary>
/// Structure for how a lump can be read and written from stream.
///
/// Doesn't store header information i.e. offset/length.
/// </summary>
[JsonConverter(typeof(JsonSubtypes), "Class")]
public abstract class Lump(BspFile parent)
{
    [JsonIgnore]
    public BspFile Parent { get; set; } = parent;

    public int Version { get; set; }

    public int Flags { get; set; }

    public virtual bool IsCompressed { get; set; }

    public abstract void Read(BinaryReader reader, long length);

    public abstract void Write(Stream stream);

    public abstract bool Empty { get; }
}
