namespace Lumper.Lib.BSP.IO;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bsp.Enum;
using Enum;
using Lumps;
using Lumps.BspLumps;
using Lumps.GameLumps;
using Newtonsoft.Json;
using NLog;

public sealed class GameLumpWriter(GameLump gameLump, Stream output) : LumpWriter(output)
{
    [JsonIgnore]
    private readonly GameLump _gameLump = gameLump;

    private readonly long _startPos = output.Position;
    private long LumpDataStart { get; set; }
    private long LumpDataEnd { get; set; }
    public List<GameLumpHeader> LumpHeaders { get; set; } = [];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public void Save()
    {
        // The last gamelump header should be 0 so we add a empty lump at the end
        _gameLump.Lumps.TryAdd(0, null);

        LumpDataStart = BaseStream.Position + 4 /* gamelump count 32bit int */ +
                        (_gameLump.Lumps.Count * GameLumpHeader.StructureSize);
        WriteAllLumps();
        WriteHeader();
    }

    private void WriteHeader()
    {
        Seek((int)_startPos, SeekOrigin.Begin);
        Write(_gameLump.Lumps.Count);
        foreach (GameLumpHeader header in LumpHeaders)
        {
            Write(header.Id);
            Write(header.Flags);
            Write(header.Version);
            Write(header.FileOfs);
            Write(header.FileLen);
        }
            throw new NotImplementedException("Failed to write GameLump header: bad length");

        if (LumpHeaders.Count != 0 && BaseStream.Position != LumpDataStart)

        BaseStream.Seek(LumpDataEnd, SeekOrigin.Begin);
    }

    private void WriteAllLumps()
    {
        Seek((int)LumpDataStart, SeekOrigin.Begin);
        foreach ((GameLumpType key, Lump? lump) in _gameLump.Lumps)
        {
            if (key == 0 || lump is null)
                continue;

            LumpHeaderInfo newHeaderInfo = Write(lump);

            // TODO: meh
            lump.Version = lump is Sprp sprp
                ? (ushort)sprp.StaticProps.GetVersion()
                : (ushort)lump.Version;

            LumpHeaders.Add(new GameLumpHeader(newHeaderInfo, (ushort)lump.Version, (int)key));
        }

        if (LumpHeaders.Count != 0 && LumpHeaders.Last().Id != 0)
            LumpHeaders.Add(new GameLumpHeader());

        LumpDataEnd = (int)BaseStream.Position;
    }
}
