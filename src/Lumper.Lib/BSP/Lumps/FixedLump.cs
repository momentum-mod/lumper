namespace Lumper.Lib.BSP.Lumps;

using System.Collections.Generic;
using System.IO;

// Lumps which contain a list/array of data U with fixed length
public abstract class FixedLump<T, TData>(BspFile parent) : ManagedLump<T>(parent)
    where T : System.Enum
{
    public List<TData> Data { get; set; } = [];

    public abstract int StructureSize { get; }

    protected abstract void ReadItem(BinaryReader reader);
    protected abstract void WriteItem(BinaryWriter writer, int index);

    public override void Read(BinaryReader reader, long length)
    {
        if (length % StructureSize != 0)
            throw new InvalidDataException($"{GetType().Name}: funny lump size ({length} / {StructureSize})");

        for (var i = 0; i < length / StructureSize; i++)
            ReadItem(reader);
    }

    public override void Write(Stream stream)
    {
        var w = new BinaryWriter(stream);
        for (var i = 0; i < Data.Count; i++)
            WriteItem(w, i);
    }

    public override bool Empty() => Data.Count == 0;

}
