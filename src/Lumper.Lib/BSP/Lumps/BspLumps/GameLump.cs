namespace Lumper.Lib.BSP.Lumps.BspLumps;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bsp.Enum;
using Enum;
using IO;
using Lumps;

public class GameLump(BspFile parent) : ManagedLump<BspLumpType>(parent)
{
    public GameLumpReader? Reader { get; private set; }

    public Dictionary<GameLumpType, Lump<GameLumpType>?> Lumps { get; } = [];

    public override bool IsCompressed
    {
        get => false;
        set { } // Deliberately left empty
    }

    public T? GetLump<T>() where T : Lump<GameLumpType>
        => (T?)Lumps.Values.First(x => x?.GetType() == typeof(T));

    public Lump<GameLumpType>? GetLump(GameLumpType lumpType) => Lumps[lumpType];

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
    {
        Reader = new GameLumpReader(this, reader, length, handler);
        Reader.Load();
    }

    public override void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null)
    {
        var gameLumpWriter = new GameLumpWriter(this, stream, handler, compression ?? DesiredCompression.Unchanged);
        gameLumpWriter.Save();
    }

    public override bool Empty => Lumps.Count == 0;
}
