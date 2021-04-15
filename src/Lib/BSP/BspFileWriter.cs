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

            foreach (var lump in _bsp.Lumps)
            {
                if (lump is ManagedLump)
                    WriteManagedLump((ManagedLump) lump);
                else
                    WriteUnmanagedLump((UnmanagedLump) lump);
            }
        }

        private void WriteManagedLump(ManagedLump lump)
        {
            var startPosition = (int) _stream.Position;

            lump.Write(_writer);

            lump.Offset = startPosition;
            lump.Length = (int) _stream.Position - startPosition;
        }

        private void WriteUnmanagedLump(Lump lump)
        {
            var startPosition = (int) _stream.Position;

            _bsp.reader.CopyLumpStream(lump, _stream);

            if (_stream.Position - startPosition != lump.Length) throw new InvalidDataException("Lump data is wrong length!");

            lump.Offset = startPosition;
        }

        private void ConstructTexDataLumps()
        {
            // TODO: this is shit
            // TODO: check obeys source limits
            List<string> texStrings = new();

            var texdata = _bsp.GetLump<TexDataLump>().Data;

            List<int> stringTable = new();
            var stringData = new char[256 * 1000];
            var pos = 0;

            // loop through every texture name
            foreach (var tex in texdata)
            {
                // at start of texture string, put its loc in stringtable
                stringTable.Add(pos);

                tex.StringTablePointer = pos;

                foreach (var c in tex.TexName)
                {
                    stringData[pos] = c;
                    pos++;
                }
                // string includes a null byte at end, right?
                // stringData[pos] = '\0';
                // pos++;
            }

            var finalArray = new byte[pos];

            for (var i = 0; i < pos; i++)
            {
                finalArray[i] = (byte) stringData[i];
            }
            
            _bsp.GetLump<TexDataStringDataLump>().Data = finalArray;
            _bsp.GetLump<TexDataStringTableLump>().Data = stringTable;

        }

        public void Dispose() => _writer.Dispose();
    }
}