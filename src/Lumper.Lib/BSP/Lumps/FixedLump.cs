namespace Lumper.Lib.Bsp.Lumps;

using System.Collections.Generic;
using System.IO;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;

// Lumps which contain a list/array of data U with fixed length
public abstract class FixedLump<T, TData>(BspFile parent) : ManagedLump<T>(parent)
    where T : System.Enum
{
    public List<TData> Data { get; set; } = [];

    public abstract int StructureSize { get; }

    protected abstract void ReadItem(BinaryReader reader);

    protected abstract void WriteItem(BinaryWriter writer, int index);

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
    {
        if (length % StructureSize != 0)
            throw new InvalidDataException($"{GetType().Name}: funny lump size ({length} / {StructureSize})");

        for (int i = 0; i < length / StructureSize; i++)
        {
            if (handler?.Cancelled ?? false)
                return;

            ReadItem(reader);
        }
    }

    public override void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null)
    {
        var w = new BinaryWriter(stream);
        for (int i = 0; i < Data.Count; i++)
        {
            if (handler?.Cancelled ?? false)
                return;

            WriteItem(w, i);
        }
    }

    public override bool Empty => Data.Count == 0;
}
