namespace Lumper.Lib.Bsp.Lumps.GameLumps;

using System;
using System.IO;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using NLog;

public class Sprp(BspFile parent) : ManagedLump<GameLumpType>(parent)
{
    public StaticPropDictLump StaticPropsDict { get; set; } = null!;
    public StaticPropLeafLump StaticPropsLeaf { get; set; } = null!;
    public StaticPropLump StaticProps { get; set; } = null!;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
    {
        var startPos = reader.BaseStream.Position;

        var dictEntries = reader.ReadInt32();
        StaticPropsDict = new StaticPropDictLump(Parent);
        StaticPropsDict.Read(reader, dictEntries * StaticPropsDict.StructureSize);

        var leafEntries = reader.ReadInt32();
        StaticPropsLeaf = new StaticPropLeafLump(Parent) { Version = Version };
        StaticPropsLeaf.Read(reader, leafEntries * StaticPropsLeaf.StructureSize);

        var entries = reader.ReadInt32();

        StaticProps = new StaticPropLump(Parent);
        StaticProps.SetVersion(Version);

        var remainingLength = (int)(length - (reader.BaseStream.Position - startPos));

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
            var tmpLength = entries * StaticProps.StructureSize;
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

        w.Write(StaticPropsDict.Data.Count);
        StaticPropsDict.Write(w.BaseStream);

        w.Write(StaticPropsLeaf.Data.Count);
        StaticPropsLeaf.Write(w.BaseStream);

        w.Write(StaticProps.Data.Count);
        StaticProps.Write(w.BaseStream);
    }

    public override bool Empty => StaticProps.Empty && StaticPropsDict.Empty && StaticPropsLeaf.Empty;
}
