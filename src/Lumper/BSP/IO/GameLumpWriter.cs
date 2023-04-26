using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Lumps.GameLumps;

namespace Lumper.Lib.BSP.IO
{
    public sealed class GameLumpWriter : LumpWriter
    {
        [JsonIgnore]
        private readonly GameLump _gameLump;

        private readonly long _startPos;
        private long LumpdataStart { get; set; }
        private long LumpdataEnd { get; set; }
        public List<GameLumpHeader> LumpHeaders { get; set; } = new();
        public GameLumpWriter(GameLump gameLump, Stream output) : base(output)
        {
            _gameLump = gameLump;
            _startPos = output.Position;
        }

        public void Save()
        {
            //the last gamelump header should be 0 so we add a empty lump at the end
            if (!_gameLump.Lumps.ContainsKey(0))
                _gameLump.Lumps.Add(0, null);
            LumpdataStart = BaseStream.Position + 4 /*gamelump count 32bit int*/ + _gameLump.Lumps.Count * GameLumpHeader.StructureSize;
            WriteAllLumps();
            WriteHeader();
        }

        private void WriteHeader()
        {
            Seek((int)_startPos, SeekOrigin.Begin);
            Write((int)_gameLump.Lumps.Count);
            foreach (var header in LumpHeaders)
            {
                Write(header.Id);
                Write(header.Flags);
                Write(header.Version);
                Write(header.FileOfs);
                Write(header.FileLen);
            }
            if (LumpHeaders.Any() && BaseStream.Position != LumpdataStart)
                throw new NotImplementedException("Failed to write GameLump header: bad length");
            BaseStream.Seek(LumpdataEnd, SeekOrigin.Begin);
        }

        private void WriteAllLumps()
        {
            Seek((int)LumpdataStart, SeekOrigin.Begin);
            foreach (var entry in _gameLump.Lumps)
            {
                if (entry.Key == 0)
                    continue;
                Lump lump = entry.Value;
                LumpHeader newHeader = Write(lump);

                //todo meh
                if (entry.Value is Sprp sprp)
                    lump.Version = (ushort)sprp.StaticProps.GetVersion();
                else
                    lump.Version = (ushort)lump.Version;

                LumpHeaders.Add(new GameLumpHeader(newHeader, (ushort)lump.Version, (int)entry.Key));
            }
            if (LumpHeaders.Any() && LumpHeaders.Last().Id != 0)
                LumpHeaders.Add(new GameLumpHeader());

            LumpdataEnd = (int)BaseStream.Position;
        }
    }
}