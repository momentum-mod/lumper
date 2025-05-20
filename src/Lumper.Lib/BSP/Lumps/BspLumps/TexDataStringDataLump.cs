namespace Lumper.Lib.Bsp.Lumps.BspLumps;

using System;
using System.IO;
using System.Linq;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using Lumper.Lib.Bsp.Lumps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TexDataStringDataLump(BspFile parent) : ManagedLump<BspLumpType>(parent)
{
    [JsonConverter(typeof(ByteArrayJsonConverter))]
    private byte[] _data = null!;

    public byte[] Data
    {
        get => _data;
        private set => _data = value;
    }

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
    {
        Data = reader.ReadBytes((int)length);
    }

    public override void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null)
    {
        stream.Write(Data, 0, Data.Length);
    }

    public override bool Empty => Data.Length <= 0;

    public void Resize(int newSize)
    {
        Array.Resize(ref _data, newSize);
    }
}

public class ByteArrayJsonConverter : JsonConverter<byte[]>
{
    public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
    {
        if (value is null)
            return;

        JArray.FromObject(value.Select(x => (short)x)).WriteTo(writer);
    }

    public override byte[] ReadJson(
        JsonReader reader,
        Type objectType,
        byte[]? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
    }

    public override bool CanRead => false;
}
