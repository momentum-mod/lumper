namespace Lumper.Lib.Bsp.Lumps;

using System.IO;
using JsonSubTypes;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using Newtonsoft.Json;

/// <summary>
/// Lump of a specific <see cref="BspLumpType"/>
/// </summary>
public abstract class Lump<T>(BspFile parent) : Lump(parent)
    where T : System.Enum
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

    public abstract void Read(BinaryReader reader, long length, IoHandler? handler = null);

    public abstract void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null);

    public abstract bool Empty { get; }
}
