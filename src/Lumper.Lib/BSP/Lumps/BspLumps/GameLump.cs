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
    [JsonIgnore]
    public GameLumpReader Reader { get; private set; } = null!;

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
        Reader = new GameLumpReader(this, reader, length, handler);
        Reader.Load();
    }

    public void Write(Stream stream, IoHandler handler, DesiredCompression compression)
    {
        var gameLumpWriter = new GameLumpWriter(this, stream, handler, compression);
        gameLumpWriter.Save();
    }

    public override bool Empty => Lumps.Count == 0;
}
