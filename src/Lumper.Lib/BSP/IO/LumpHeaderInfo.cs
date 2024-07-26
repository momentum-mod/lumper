namespace Lumper.Lib.BSP.IO;

using JsonSubTypes;
using Newtonsoft.Json;

/// <summary>
/// Helper record for loading/saving lump offset and length data
///
/// Provide -1 for CompressedLength to mark the lump as uncompressed.
/// </summary>
[JsonConverter(typeof(JsonSubtypes), "Type")]
public record LumpHeaderInfo
{
    public long Offset { get; set; }
    public long UncompressedLength { get; set; }
    public long CompressedLength { get; set; } = -1;
    public bool Compressed => CompressedLength >= 0;
    public long Length => Compressed ? CompressedLength : UncompressedLength;
}
