using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public abstract class FixedLump<T> : ManagedLump
    {
        public List<T> Data { get; set; } = new();

        protected abstract int StructureSize { get; }

        protected abstract void ReadItem(BinaryReader reader);
        protected abstract void WriteItem(BinaryWriter writer, int index);

        public override void Read(BinaryReader reader)
        {
            // TODO: checks and shit
            for (var i = 0; i < Length / StructureSize; i++)
            {
                ReadItem(reader);
            }
        }

        public override void Write(BinaryWriter writer)
        {
            for (var i = 0; i < Data.Count; i++)
            {
                WriteItem(writer, i);
            }
        }

        protected FixedLump(BspFile parent) : base(parent)
        {
        }
    }
}