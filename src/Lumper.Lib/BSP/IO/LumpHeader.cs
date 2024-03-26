namespace Lumper.Lib.BSP.IO;
using JsonSubTypes;
using Newtonsoft.Json;

// helper class for saving lump offset an length data
[JsonConverter(typeof(JsonSubtypes), "Type")]
public class LumpHeader(long offset, long uncompressedLength, long compressedLength)
{
    public LumpHeader() : this(-1, -1, -1)
    { }

    public long Offset { get; set; } = offset;
    public long UncompressedLength { get; set; } = uncompressedLength;
    public long CompressedLength { get; set; } = compressedLength;

    public bool Compressed => CompressedLength >= 0;
    // The actual length
    public long Length => Compressed ? CompressedLength : UncompressedLength;
}
