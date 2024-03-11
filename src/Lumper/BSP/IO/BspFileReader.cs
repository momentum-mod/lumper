using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace Lumper.Lib.BSP.IO
{
    public class BspFileReader : LumpReader
    {
        [JsonIgnore]
        private readonly BspFile _bsp;

        [JsonProperty]
        public IReadOnlyDictionary<BspLumpType, LumpHeader> Headers
        {
            get => Lumps.ToDictionary(
                x => (x.Item1 is Lump<BspLumpType> lump
                                ? lump.Type
                                : BspLumpType.Unknown),
                x => x.Item2);
        }

        public BspFileReader(BspFile file, Stream input) : base(input)
        {
            _bsp = file;
        }

        public void Load()
        {
            Lumps.Clear();
            _bsp.Lumps.Clear();
            ReadHeader();
            LoadAll();
            ResolveTexNames();
            ResolveTexData();
        }

        protected override void ReadHeader()
        {
            if (BaseStream.Position != 0)
                BaseStream.Seek(0, SeekOrigin.Begin);

            var ident = ReadBytes(4);

            if (Encoding.Default.GetString(ident) != "VBSP")
                throw new InvalidDataException("File doesn't look like a VBSP");

            _bsp.Version = ReadInt32();
            _logger.LogInformation($"BSP version: {_bsp.Version}");

            for (var i = 0; i < BspFile.HeaderLumps; i++)
            {
                var type = (BspLumpType)i;

                Lump<BspLumpType> lump = type switch
                {
                    BspLumpType.Entities => new EntityLump(_bsp),
                    BspLumpType.Texinfo => new TexInfoLump(_bsp),
                    BspLumpType.Texdata => new TexDataLump(_bsp),
                    BspLumpType.Texdata_string_table => new TexDataStringTableLump(_bsp),
                    BspLumpType.Texdata_string_data => new TexDataStringDataLump(_bsp),
                    BspLumpType.Pakfile => new PakFileLump(_bsp),
                    BspLumpType.Game_lump => new GameLump(_bsp),
                    _ => new UnmanagedLump<BspLumpType>(_bsp)
                };
                LumpHeader lumpHeader = new();

                lump.Type = type;
                lumpHeader.Offset = ReadInt32();
                int length = ReadInt32();
                lump.Version = ReadInt32();
                int fourCc = ReadInt32();
                if (fourCc == 0)
                {
                    lumpHeader.CompressedLength = -1;
                    lumpHeader.UncompressedLength = length;
                }
                else
                {
                    lumpHeader.CompressedLength = length;
                    lumpHeader.UncompressedLength = fourCc;
                }

                _logger.LogInformation($"Lump {type}({(int)type})"
                                  + $"\toffset: {lumpHeader.Offset}"
                                  + $"\t length: {length}"
                                  + $"\t Version: {lump.Version}"
                                  + $"\t FourCc: {fourCc}");

                _bsp.Lumps.Add(type, lump);
                Lumps.Add(new Tuple<Lump, LumpHeader>(lump, lumpHeader));
            }

            _bsp.Revision = ReadInt32();

            UpdateGameLumpLength();

            SortLumps();

            if (CheckOverlapping())
                throw new InvalidDataException("Some lumps are overlapping. Check logging for details.");
        }

        //finding the real gamelump length by looking at the next lump
        private void UpdateGameLumpLength()
        {
            Lump gameLump = null;
            LumpHeader gameLumpHeader = null;
            foreach (var l in Lumps.OrderBy(x => x.Item2.Offset))
            {
                Lump lump = l.Item1;
                LumpHeader header = l.Item2;
                if (lump is GameLump)
                {
                    gameLump = lump;
                    gameLumpHeader = header;
                    if (gameLumpHeader.Length == 0 && gameLumpHeader.Offset == 0)
                    {
                        _logger.LogInformation("GameLump length and offset 0 .. won't set new length");
                        break;
                    }
                }
                else if (gameLump is not null && header.Offset != 0 && header.Offset != gameLumpHeader.Offset)
                {
                    gameLumpHeader.UncompressedLength = header.Offset - gameLumpHeader.Offset;
                    _logger.LogInformation($"Changed gamelump length to {gameLumpHeader.Length}");
                    break;
                }
            }
        }

        //sort by offset so the output file looks more like the input
        private void SortLumps()
        {
            Dictionary<BspLumpType, Lump<BspLumpType>> newLumps = new();
            foreach (var l in Lumps.OrderBy(x => x.Item2.Offset))
            {
                var temp = _bsp.Lumps.First(x => x.Value == l.Item1);
                newLumps.Add(temp.Key, temp.Value);
                _bsp.Lumps.Remove(temp.Key);
            }

            if (_bsp.Lumps.Any())
                throw new InvalidDataException("SortLumps error: BSP lumps and reader headers didn't match!");
            _bsp.Lumps = newLumps;
        }

        //for testing
        private bool CheckOverlapping()
        {
            var ret = false;
            Lump<BspLumpType> prevLump = null;
            LumpHeader prevHeader = null;
            bool first = true;
            foreach (var l in Lumps.OrderBy(x => x.Item2.Offset))
            {
                var lump = (Lump<BspLumpType>)l.Item1;
                LumpHeader header = l.Item2;
                if (first)
                {
                    first = false;
                    prevLump = lump;
                    prevHeader = header;
                }
                else if (header.Length > 0)
                {
                    long prevEnd = prevHeader.Offset + prevHeader.Length;
                    if (header.Offset < prevEnd)
                    {
                        _logger.LogInformation($"Lumps {prevLump.Type} and {lump.Type} overlapping");
                        if (prevLump.Type == BspLumpType.Game_lump)
                            _logger.LogInformation("but the previous lump was GAME_LUMP and the length is a lie");
                        else
                            ret = true;
                    }
                    else if (header.Offset > prevEnd)
                    {
                        _logger.LogInformation($"Space between lumps {prevLump.Type} {prevEnd} <-- {header.Offset - prevEnd} --> {header.Offset} {lump.Type}");
                    }

                    if (header.Offset + header.Length >= prevEnd)
                    {
                        prevLump = lump;
                        prevHeader = header;
                    }
                }
            }
            return ret;
        }

        private void ResolveTexNames()
        {
            var texDataLump = _bsp.GetLump<TexDataLump>();
            foreach (var texture in texDataLump.Data)
            {
                var name = new StringBuilder();
                var texDataStringTableLump = _bsp.GetLump<TexDataStringTableLump>();
                var stringTableOffset = texDataStringTableLump.Data[texture.StringTablePointer];
                var texDataStringDataLump = _bsp.GetLump<TexDataStringDataLump>();

                var end = Array.FindIndex(texDataStringDataLump.Data, stringTableOffset, x => x == 0);
                if (end < 0)
                {
                    end = texDataStringDataLump.Data.Length;
                    _logger.LogWarning("WARNING: didn't find null at the end of texture string");
                }
                texture.TexName = end > 0
                    ? TexDataStringDataLump.TextureNameEncoding.GetString(
                        texDataStringDataLump.Data,
                        stringTableOffset,
                        end - stringTableOffset)
                    : "";
            }
        }

        private void ResolveTexData()
        {
            var texInfoLump = _bsp.GetLump<TexInfoLump>();
            foreach (var texInfo in texInfoLump.Data)
            {
                var texDataLump = _bsp.GetLump<TexDataLump>();
                texInfo.TexData = texDataLump.Data[texInfo.TexDataPointer];
            }
        }
    }
}
