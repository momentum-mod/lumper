using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lumper.Lib.BSP.Lumps.BspLumps;

namespace Lumper.Lib.BSP.IO
{
    public class BspFileWriter : LumpWriter
    {
        private readonly BspFile _bsp;
        public Dictionary<BspLumpType, BspLumpHeader> LumpHeaders { get; set; } = new();
        public BspFileWriter(BspFile file, Stream output) : base(output)
        {
            _bsp = file;
        }

        public void Save()
        {
            ConstructTexDataLumps();
            WriteAllLumps();
            WriteHeader();
        }

        private void WriteHeader()
        {
            Seek(0, SeekOrigin.Begin);

            Write(Encoding.Default.GetBytes("VBSP"));
            Write(_bsp.Version);

            foreach (var lump in LumpHeaders.OrderBy(x => x.Key).Select(x => x.Value))
            {
                Write(lump.Offset);
                Write(lump.Length);
                Write(lump.Version);
                Write(lump.FourCc);
            }

            Write(_bsp.Revision);
        }

        private void WriteAllLumps()
        {
            // Seek past the header
            BaseStream.Seek(BspFile.HeaderSize, SeekOrigin.Begin);

            int startPosition = 0;
            List<BspLumpType> lumpTypes = _bsp.Lumps.Select(x => x.Key).ToList();
            foreach (var lumpType in lumpTypes)
            {
                var lump = _bsp.Lumps[lumpType];
                if (!lump.Empty())
                {
                    //Lump offsets (and their corresponding data lumps) are always rounded up to the nearest 4-byte boundary, though the lump length may not be. 
                    int padTo = (lumpType == BspLumpType.Physlevel) ? 16 : 4;
                    var pad = new byte[(padTo - BaseStream.Position % padTo)];
                    if (pad.Length != padTo)
                        Write(pad);
                    startPosition = (int)BaseStream.Position;
                }

                if ((lump is GameLump || lump is PakFileLump) && lump.Compress)
                {
                    Console.WriteLine($"Let's not compress {lump.GetType().Name} .. it's a silly place");
                    lump.Compress = false;
                }
                LumpHeader newHeader = Write(lump);
                LumpHeaders[lumpType] = new BspLumpHeader(newHeader, lump.Version);

                Console.WriteLine($"Lump {lumpType}({(int)lumpType})\n\t{newHeader.Offset}\n\t{newHeader.Length}");
            }
        }
        private void ConstructTexDataLumps()
        {
            // TODO: check obeys source limits
            List<string> texStrings = new();

            var texData = _bsp.GetLump<TexDataLump>().Data;

            List<int> stringTable = new();
            var pos = 0;

            // loop through every texture name
            var texDataStringDataLump = _bsp.GetLump<TexDataStringDataLump>();
            var sum = texData.Sum(x => TexDataStringDataLump.TextureNameEncoding.GetByteCount(x.TexName) + 1);
            Array.Resize(ref texDataStringDataLump.Data, sum);
            foreach (var tex in texData)
            {
                // at start of texture string, put its loc in stringtable
                stringTable.Add(pos);

                tex.StringTablePointer = stringTable.Count - 1;

                var bytes = TexDataStringDataLump.TextureNameEncoding.GetBytes(tex.TexName);
                Array.Copy(bytes, 0, texDataStringDataLump.Data, pos, bytes.Length);
                pos += bytes.Length;
                texDataStringDataLump.Data[pos] = 0;
                pos++;
            }

            _bsp.GetLump<TexDataStringTableLump>().Data = stringTable;
        }
    }
}