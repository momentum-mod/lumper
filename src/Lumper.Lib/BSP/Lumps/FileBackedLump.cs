namespace Lumper.Lib.Bsp.Lumps;

using System.IO;

public interface IFileBackedLump
{
    public Stream DataStream { get; set; }

    public long DataStreamOffset { get; set; }

    public long DataStreamLength { get; set; }
}
