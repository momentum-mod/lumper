namespace Lumper.Lib.BSP.Lumps.BspLumps;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TexDataStringDataLump(BspFile parent) : ManagedLump<BspLumpType>(parent)
{
    public static readonly Encoding TextureNameEncoding = Encoding.UTF8;
    [JsonConverter(typeof(ByteArrayJsonConverter))]
    public byte[] Data;

    public override void Read(BinaryReader reader, long length) => Data = reader.ReadBytes((int)length);

    public override void Write(Stream stream) => stream.Write(Data, 0, Data.Length);

    public override bool Empty() => Data.Length <= 0;
}

public class ByteArrayJsonConverter : JsonConverter<byte[]>
{
    public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
    {
        var o = JArray.FromObject(value.Select(x => (short)x));
        o.WriteTo(writer);
    }

    public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");

    public override bool CanRead => false;
}
