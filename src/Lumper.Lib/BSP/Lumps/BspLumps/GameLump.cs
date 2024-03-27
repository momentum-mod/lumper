namespace Lumper.Lib.BSP.Lumps.BspLumps;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.BSP.IO;
using Lumper.Lib.BSP.Lumps.GameLumps;

public class GameLump : ManagedLump<BspLumpType>
{
    public GameLumpReader? Reader { get; private set; }
    public Dictionary<GameLumpType, Lump?> Lumps { get; set; } = [];

    public GameLump(BspFile parent) : base(parent) => Compress = false;

    public T? GetLump<T>() where T : Lump<GameLumpType> =>
        (T?)Lumps.Values.First(x => x?.GetType() == typeof(T));

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

    public override bool Empty() => Lumps.Count == 0;
}
