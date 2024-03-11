using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace Lumper.Lib.BSP.Lumps.GameLumps
{
    public class StaticPropDictLump : FixedLump<GameLumpType, string>
    {
        public override int StructureSize => 128;

        protected override void ReadItem(BinaryReader reader)
        {
            Data.Add(new string(reader.ReadChars(StructureSize)));
        }
        protected override void WriteItem(BinaryWriter writer, int index)
        {
            var b = new byte[StructureSize];
            var value = Encoding.ASCII.GetBytes(Data[index]);
            var count = value.Length;
            if (count > StructureSize)
            {
                var logger = LumperLoggerFactory.GetInstance().CreateLogger(GetType());
                logger.LogWarning($"WARNING: {this.GetType().Name} string to long");
                count = StructureSize;
            }
            Array.Copy(value, b, count);
            writer.Write(b);
        }

        public StaticPropDictLump(BspFile parent) : base(parent)
        {
        }
    }
}