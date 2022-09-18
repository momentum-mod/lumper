using System;
using System.IO;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Lumps.GameLumps;

namespace Lumper.Lib.BSP.IO
{
    public sealed class GameLumpReader : LumpReader
    {
        private readonly GameLump _gameLump;
        private readonly long _length;
        public GameLumpReader(GameLump gamelump, BinaryReader reader, long length)
            : this(gamelump, reader.BaseStream, length)
        { }
        public GameLumpReader(GameLump gamelump, Stream input, long length)
            : base(input)
        {
            _gameLump = gamelump;
            _length = length;
        }

        public void Load()
        {
            Lumps.Clear();
            _gameLump.Lumps.Clear();
            ReadHeader();
            LoadAll();
        }

        protected override void ReadHeader()
        {
            long startPos = BaseStream.Position;
            var count = ReadInt32();
            LumpHeader prevHeader = null;
            for (var i = 0; i < count; i++)
            {
                var type = (GameLumpType)ReadInt32();

                Lump<GameLumpType> lump = type switch
                {
                    GameLumpType.sprp => new Sprp(_gameLump.Parent),
                    _ => new UnmanagedLump<GameLumpType>(_gameLump.Parent)
                };
                lump.Type = type;
                lump.Flags = ReadUInt16();
                lump.Version = ReadUInt16();

                var header = new LumpHeader()
                {
                    Offset = ReadInt32(),
                    UncompressedLength = ReadInt32(),
                };

                if (prevHeader != null)
                {
                    var actualLength = header.Offset - prevHeader.Offset;
                    if (actualLength < 0)
                        actualLength = _length - (prevHeader.Offset + prevHeader.Length - startPos);

                    if (actualLength == prevHeader.UncompressedLength)
                        prevHeader.CompressedLength = -1;
                    else
                        prevHeader.CompressedLength = actualLength;
                }

                Lumps.Add(new Tuple<Lump, LumpHeader>(lump, header));

                if (_gameLump.Lumps.ContainsKey(type))
                    Console.WriteLine($"WARNING: key {type} already in gamelumps .. skipping");
                else
                    _gameLump.Lumps.Add(type, lump);

                Console.WriteLine($"Gamelump " + _gameLump.Lumps.Count);
                Console.WriteLine($"\tId: {type} {(int)type}");
                Console.WriteLine($"\tFlags: {lump.Flags}");
                Console.WriteLine($"\tFileofs: {header.Offset}");
                Console.WriteLine($"\tFilelen: {header.UncompressedLength}");

                prevHeader = header;
            }
            // won't set compressedLength on the last entry 
            // but it should be 0 and if its not we don't know the length 
            // so its probably not compressed
        }
    }
}
