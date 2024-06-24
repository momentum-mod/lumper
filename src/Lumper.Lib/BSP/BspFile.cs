namespace Lumper.Lib.BSP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.BSP.IO;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Newtonsoft.Json;

public class BspFile
{
    public const int HeaderLumps = 64;
    public const int HeaderSize = 1036;
    public const int MaxLumps = 128;

    [JsonIgnore]
    public string? FilePath { get; private set; }
    public string Name { get; private set; } = null!;
    public int Revision { get; set; }
    public int Version { get; set; }

    protected MemoryStream Stream { get; private set; } = null!;

    public Dictionary<BspLumpType, Lump<BspLumpType>> Lumps { get; set; } = [];
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // The nullability of all sorts of parts of this class and its members is based on the assumption
    // that an actual BSP has been loaded.
    // We don't support or plan to support creating BSPs from scratch, so private ctor blocks
    // creating a BspFile instance without loading an actual BSP.
    private BspFile() { }

    public BspFile(string path) => Load(path);

    public void Load(string path)
    {
        // TODO: loads of error handling
        Name = Path.GetFileNameWithoutExtension(path);
        var filePath = Uri.UnescapeDataString(Path.GetFullPath(path));
        using FileStream stream = File.OpenRead(filePath);
        Load(stream);
        FilePath = filePath; // Set this at the end because Load(stream) resets it
    }

    public void Load(Stream stream)
    {
        FilePath = null;
        Stream.Dispose();
        Stream = new MemoryStream();
        stream.CopyTo(Stream);
        var reader = new BspFileReader(this, Stream);
        reader.Load();
    }

    public void Save(string path)
    {
        if (path == FilePath)
            throw new IOException("Can't write BSP to the same file");
        using FileStream stream = File.Open(path, FileMode.Create);
        Save(stream);
    }

    public void Save(Stream stream)
    {
        using var writer = new BspFileWriter(this, stream);
        writer.Save();
    }

    public T GetLump<T>() where T : Lump<BspLumpType>
    {
        // If you add something here, also add it to the BspReader
        Dictionary<Type, BspLumpType> typeMap = new()
        {
            { typeof(EntityLump), BspLumpType.Entities },
            { typeof(TexInfoLump), BspLumpType.Texinfo },
            { typeof(TexDataLump), BspLumpType.Texdata },
            { typeof(TexDataStringTableLump), BspLumpType.TexdataStringTable },
            { typeof(TexDataStringDataLump), BspLumpType.TexdataStringData },
            { typeof(PakFileLump), BspLumpType.Pakfile },
            { typeof(GameLump), BspLumpType.GameLump }
        };

        if (typeMap.ContainsKey(typeof(T)))
        {
            return (T)Lumps[typeMap[typeof(T)]];
        }
        IEnumerable<KeyValuePair<BspLumpType, Lump<BspLumpType>>> tLumps = Lumps.Where(x => x.Value.GetType() == typeof(T));
        return (T)tLumps.Select(x => x.Value).First();
    }

    public Lump<BspLumpType> GetLump(BspLumpType lumpType) => Lumps[lumpType];

    public void ToJson(bool sortLumps,
        bool sortProperties,
        bool ignoreOffset)
    {
        var dir = Path.GetDirectoryName(FilePath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(FilePath);
        var path = $"{dir}/{name}.json";
        using var fileStream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write);

        ToJson(fileStream, sortLumps, sortProperties, ignoreOffset);
        Logger.Info($"Dumped JSON to {path}");
    }

    public void ToJson(Stream stream, bool sortLumps, bool sortProperties, bool ignoreOffset)
    {
        if (sortLumps)
        {
            Lumps = Lumps
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        using var bspStream = new MemoryStream();
        using var bspWriter = new BspFileWriter(this, bspStream);
        bspWriter.Save();

        var jsonSerializerSettings = new JsonSerializerSettings {
            ContractResolver = new JsonContractResolver {
                SortProperties = sortProperties, IgnoreOffset = ignoreOffset
            },
            Formatting = Formatting.Indented
        };

        var serializer = JsonSerializer.Create(jsonSerializerSettings);
        using var sw = new StreamWriter(stream);
        using var writer = new JsonTextWriter(sw);
        serializer.Serialize(writer, new { Bsp = this, Writer = bspWriter });
    }
}
