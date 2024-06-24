namespace Lumper.Lib.BSP.Lumps.BspLumps;

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
    }
    public override void Read(BinaryReader reader, long length)
    {
        Reader = new GameLumpReader(this, reader, length);
        Reader.Load();
    }

    public override void Write(Stream stream)
    {
        var gameLumpWriter = new GameLumpWriter(this, stream);
        gameLumpWriter.Save();
    }

    public override bool Empty => Lumps.Count == 0;
}
