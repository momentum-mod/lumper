namespace Lumper.Lib.BSP.Lumps.BspLumps;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bsp.Enum;
using Enum;
using IO;
using Lumps;

public class GameLump : ManagedLump<BspLumpType>
{
    public T GetLump<T>() where T : Lump<GameLumpType>
    {
        Dictionary<System.Type, GameLumpType> typeMap = new()
        {
            { typeof(Sprp), GameLumpType.sprp }
        };
    public GameLumpReader? Reader { get; private set; }

    public GameLump(BspFile parent) : base(parent) => Compress = false;
    public Dictionary<GameLumpType, Lump?> Lumps { get; } = [];

        if (typeMap.ContainsKey(typeof(T)))
        {
            return (T)Lumps[typeMap[typeof(T)]];
        }
        IEnumerable<KeyValuePair<GameLumpType, Lump>> tLumps = Lumps.Where(x => x.Value.GetType() == typeof(T));
        return (T)tLumps.Select(x => x.Value).First();
    public override bool IsCompressed
    {
        get => false;
        set { } // Deliberately left empty
    }

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
