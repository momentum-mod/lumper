namespace Lumper.Lib.BSP.Lumps;

using System.IO;

public interface IFileBackedLump
{
    public Stream DataStream { get; set; }

    public long DataStreamOffset { get; set; }

    public long DataStreamLength { get; set; }
}
