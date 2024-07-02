namespace Lumper.Lib.BSP.Lumps.BspLumps;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bsp.Enum;
using Enum;
using IO;
using Lumps;
using Newtonsoft.Json;

public class GameLump(BspFile parent) : ManagedLump<BspLumpType>(parent)
{
    public Dictionary<GameLumpType, Lump<GameLumpType>?> Lumps { get; } = [];

    public override bool IsCompressed
    {
        get => false;
        set { } // Deliberately left empty
    }

    public T? GetLump<T>() where T : Lump<GameLumpType>
        => (T?)Lumps.Values.First(x => x?.GetType() == typeof(T));

    public Lump<GameLumpType>? GetLump(GameLumpType lumpType) => Lumps[lumpType];

    // Same as PakfileLump strategy, trying to avoid overly complex inheritance nonsense
    public override void Read(BinaryReader reader, long length) => throw new NotImplementedException();
    public override void Write(Stream stream) => throw new NotImplementedException();

    public void Read(BinaryReader reader, long length, IoHandler handler)
    {
        using var glReader = new GameLumpReader(this, reader, length, handler);
        glReader.Load();
    }

    // This is a hack so we can access the writer used on last save in BspFile.SaveToFile :(
    [JsonIgnore]
    public GameLumpWriter? LastWriter { get; set; }

    public void Write(Stream stream, IoHandler handler, DesiredCompression compression)
    {
        LastWriter = new GameLumpWriter(this, stream, handler, compression);
        LastWriter.Save();
    }

    public override bool Empty => Lumps.Count == 0;
}
