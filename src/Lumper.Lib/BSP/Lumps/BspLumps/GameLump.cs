namespace Lumper.Lib.Bsp.Lumps.BspLumps;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using Lumper.Lib.Bsp.Lumps;
using Newtonsoft.Json;

public class GameLump(BspFile parent) : ManagedLump<BspLumpType>(parent)
{
    public Dictionary<GameLumpType, Lump<GameLumpType>?> Lumps { get; } = [];

    public override bool IsCompressed
    {
        get => false;
        set { } // Deliberately left empty
    }

    public T? GetLump<T>()
        where T : Lump<GameLumpType>
    {
        return Lumps.Values.OfType<T>().FirstOrDefault();
    }

    public Lump<GameLumpType>? GetLump(GameLumpType lumpType)
    {
        return Lumps[lumpType];
    }

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
    {
        using var glReader = new GameLumpReader(this, reader, length, handler);
        glReader.Load();
    }

    // This is a hack so we can access the writer used on last save in BspFile.SaveToFile :(
    [JsonIgnore]
    public GameLumpWriter? LastWriter { get; set; }

    public override void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null)
    {
        LastWriter = new GameLumpWriter(this, stream, handler, compression ?? DesiredCompression.Unchanged);
        LastWriter.Save();
    }

    public override bool Empty => Lumps.Count == 0;
}
