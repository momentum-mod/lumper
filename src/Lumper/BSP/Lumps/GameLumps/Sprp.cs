using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Lumper.Lib.BSP.Lumps.GameLumps
{
    public class Sprp : ManagedLump<GameLumpType>
    {
        public StaticPropDictLump StaticPropsDict { get; set; }
        public StaticPropLeafLump StaticPropsLeaf { get; set; }
        public StaticPropLump StaticProps { get; set; }
        public Sprp(BspFile parent) : base(parent)
        { }
        public override void Read(BinaryReader reader, long length)
        {
            var logger = LumperLoggerFactory.GetInstance().CreateLogger(GetType());

            var startPos = reader.BaseStream.Position;

            int dictEntries = reader.ReadInt32();
            StaticPropsDict = new(Parent);
            StaticPropsDict.Read(reader, dictEntries * StaticPropsDict.StructureSize);

            int leafEntries = reader.ReadInt32();
            StaticPropsLeaf = new(Parent);
            StaticPropsLeaf.Version = Version;
            StaticPropsLeaf.Read(reader, leafEntries * StaticPropsLeaf.StructureSize);

            int entries = reader.ReadInt32();
            StaticProps = new(Parent);
            int remainingLength = (int)(length - (reader.BaseStream.Position - startPos));

            StaticProps.SetVersion(Version);
            switch (StaticProps.ActualVersion)
            {
                case StaticPropVersion.V7:
                case StaticPropVersion.V10:
                    if (remainingLength % StaticProps.StructureSize != 0)
                    {
                        StaticProps.ActualVersion = StaticPropVersion.V7s;
                        logger.LogInformation($"Remaining length doesn't fit version {Version} .. trying V7*");
                    }
                    break;
            }
            if (StaticProps.ActualVersion != StaticPropVersion.Unknown)
            {
                var tmpLength = entries * StaticProps.StructureSize;
                if (tmpLength != remainingLength)
                    throw new InvalidDataException($"Funny staticprop length ({tmpLength} != {remainingLength})");
                StaticProps.Read(reader, tmpLength);
            }
            else
                throw new NotImplementedException($"Unknown staticprop version (Version: {StaticProps.Version})");
        }

        public override void Write(Stream stream)
        {
            var w = new BinaryWriter(stream);

            w.Write((int)StaticPropsDict.Data.Count);
            StaticPropsDict.Write(w.BaseStream);

            w.Write((int)StaticPropsLeaf.Data.Count);
            StaticPropsLeaf.Write(w.BaseStream);

            w.Write((int)StaticProps.Data.Count);
            StaticProps.Write(w.BaseStream);
        }

        public override bool Empty()
        {
            return StaticPropsDict.Empty()
                && StaticPropsLeaf.Empty()
                && StaticProps.Empty();
        }
    }
}