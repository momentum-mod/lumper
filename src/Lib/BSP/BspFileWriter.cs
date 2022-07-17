using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MomBspTools.Lib.BSP.Lumps;

namespace MomBspTools.Lib.BSP
{
    public sealed class BspFileWriter : IDisposable
    {
        private readonly BspFile _bsp;
        private readonly FileStream _stream;
        private readonly BinaryWriter _writer;

        public BspFileWriter(BspFile file, string path)
        {
            _bsp = file;
            _stream = File.OpenWrite(path);
            _writer = new BinaryWriter(_stream);
        }

        public void Save()
        {
            ConstructTexDataLumps();
            WriteAllLumps();
            WriteHeader();
        }

        private void WriteHeader()
        {
            _stream.Seek(0, 0);

            _writer.Write(Encoding.Default.GetBytes("VBSP"));
            _writer.Write(_bsp.Version);

            foreach (var lump in _bsp.Lumps)
            {
                _writer.Write(lump.Offset);
                _writer.Write(lump.Length);
                _writer.Write(lump.Version);
                _writer.Write(lump.FourCc);
            }

            _writer.Write(_bsp.Revision);
        }

        private void WriteAllLumps()
        {
            // Seek past the header
            _stream.Seek(BspFile.HeaderSize, 0);

            foreach (var lump in _bsp.Lumps.OrderBy(x => x.Offset))
            {
                if (lump is ManagedLump)
                    WriteManagedLump((ManagedLump)lump);
                else
                    WriteUnmanagedLump((UnmanagedLump)lump);
            }
        }

        private void WriteManagedLump(ManagedLump lump)
        {
            var startPosition = (int)_stream.Position;

            lump.Write(_writer);

            int oldOffset = lump.Offset;
            int oldLength = lump.Length;
            lump.Offset = startPosition;
            lump.Length = (int)_stream.Position - startPosition;
            Console.WriteLine($"Lump {lump.Type}({(int)lump.Type})\n\t{oldOffset}\t->\t{lump.Offset} \n\t{oldLength}\t->\t{lump.Length}");

            if (oldLength != lump.Length)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("length changed");
                Console.ResetColor();
            }
        }

        private void WriteUnmanagedLump(Lump lump)
        {
            var startPosition = (int)_stream.Position;

            _bsp.reader.CopyLumpStream(lump, _stream);

            if (_stream.Position - startPosition != lump.Length) throw new InvalidDataException("Lump data is wrong length!");

            int oldOffset = lump.Offset;
            lump.Offset = startPosition;
            Console.WriteLine($"Lump {lump.Type}({(int)lump.Type})\n\t{oldOffset}\t->\t{lump.Offset} \n\tlength: {lump.Length}");
        }

        private void ConstructTexDataLumps()
        {
            // TODO: this is shit
            // TODO: check obeys source limits
            List<string> texStrings = new();

            var texdata = _bsp.GetLump<TexDataLump>().Data;

            List<int> stringTable = new();
            var pos = 0;

            // loop through every texture name
            var texDataStringDataLump = _bsp.GetLump<TexDataStringDataLump>();
            //TODO counts the amount of char and not byts but we convert to ascii and multibyte chars print a questionmark for each byte .. maybe?
            var sum = texdata.Sum(x => x.TexName.Length + 1);
            Array.Resize(ref texDataStringDataLump.Data, sum);
            foreach (var tex in texdata)
            {
                // at start of texture string, put its loc in stringtable
                stringTable.Add(pos);

                tex.StringTablePointer = stringTable.Count - 1;

                //TODO is this ascii?
                var bytes = Encoding.ASCII.GetBytes(tex.TexName);
                Array.Copy(bytes, 0, texDataStringDataLump.Data, pos, bytes.Length);
                pos += bytes.Length;
                texDataStringDataLump.Data[pos] = 0;
                pos++;
            }

            _bsp.GetLump<TexDataStringTableLump>().Data = stringTable;

        }

        public void Dispose() => _writer.Dispose();
    }
}