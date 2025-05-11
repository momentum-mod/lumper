namespace Lumper.Lib.Bsp.Lumps.GameLumps;

using System;
using System.IO;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using NLog;

public class Sprp(BspFile parent) : ManagedLump<GameLumpType>(parent)
{
    // All are null iff Sprp is empty when reading i.e. header says length == 0
    public StaticPropDictLump? StaticPropsDict { get; set; }
    public StaticPropLeafLump? StaticPropsLeaf { get; set; }
    public StaticPropLump? StaticProps { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
    {
        long startPos = reader.BaseStream.Position;

        int dictEntries = reader.ReadInt32();
        StaticPropsDict = new StaticPropDictLump(Parent);
        StaticPropsDict.Read(reader, dictEntries * StaticPropsDict.StructureSize);

        int leafEntries = reader.ReadInt32();
        StaticPropsLeaf = new StaticPropLeafLump(Parent) { Version = Version };
        StaticPropsLeaf.Read(reader, leafEntries * StaticPropsLeaf.StructureSize);

        int entries = reader.ReadInt32();

        StaticProps = new StaticPropLump(Parent);
        StaticProps.SetVersion(Version);

        int remainingLength = (int)(length - (reader.BaseStream.Position - startPos));

        if (StaticProps.ActualVersion is StaticPropVersion.V7 or StaticPropVersion.V10)
        {
            if (Math.Abs(((float)remainingLength / StaticProps.StructureSize) - entries) > 0.0000001)
            {
                StaticProps.ActualVersion = StaticPropVersion.V7s;
                Logger.Debug($"Remaining length of staticprop lumpdoesn't fit version {Version}, trying V7s");
            }
        }

        if (StaticProps.ActualVersion != StaticPropVersion.Unknown)
        {
            int tmpLength = entries * StaticProps.StructureSize;
            if (tmpLength != remainingLength)
                throw new InvalidDataException($"Funny staticprop length ({tmpLength} != {remainingLength})");

            StaticProps.Read(reader, tmpLength, handler);
        }
        else
        {
            throw new InvalidDataException($"Unknown staticprop version (Version: {StaticProps.Version})");
        }
    }

    public override void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null)
    {
        var w = new BinaryWriter(stream);

        if (StaticPropsDict is not null)
        {
            w.Write(StaticPropsDict.Data.Count);
            StaticPropsDict.Write(w.BaseStream);
        }

        if (StaticPropsLeaf is not null)
        {
            w.Write(StaticPropsLeaf.Data.Count);
            StaticPropsLeaf.Write(w.BaseStream);
        }

        if (StaticProps is not null)
        {
            w.Write(StaticProps.Data.Count);
            StaticProps.Write(w.BaseStream);
        }
    }

    public override bool Empty => StaticProps is null && StaticPropsDict is null && StaticPropsLeaf is null;
}
