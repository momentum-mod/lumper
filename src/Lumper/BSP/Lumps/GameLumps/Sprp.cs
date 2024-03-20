namespace Lumper.Lib.BSP.Lumps.GameLumps;
using System;
using System.IO;

public class Sprp(BspFile parent) : ManagedLump<GameLumpType>(parent)
{
    public StaticPropDictLump StaticPropsDict { get; set; }
    public StaticPropLeafLump StaticPropsLeaf { get; set; }
    public StaticPropLump StaticProps { get; set; }

    public override void Read(BinaryReader reader, long length)
    {
        var startPos = reader.BaseStream.Position;

        var dictEntries = reader.ReadInt32();
        StaticPropsDict = new(Parent);
        StaticPropsDict.Read(reader, dictEntries * StaticPropsDict.StructureSize);

        var leafEntries = reader.ReadInt32();
        StaticPropsLeaf = new(Parent)
        {
            Version = Version
        };
        StaticPropsLeaf.Read(reader, leafEntries * StaticPropsLeaf.StructureSize);

        var entries = reader.ReadInt32();
        StaticProps = new(Parent);
        var remainingLength = (int)(length - (reader.BaseStream.Position - startPos));

        StaticProps.SetVersion(Version);
        switch (StaticProps.ActualVersion)
        {
            case StaticPropVersion.V7:
            case StaticPropVersion.V10:
                if (remainingLength % StaticProps.StructureSize != 0)
                {
                    StaticProps.ActualVersion = StaticPropVersion.V7s;
                    Console.WriteLine($"Remaining length doesn't fit version {Version} .. trying V7*");
                }
                break;
            case StaticPropVersion.Unknown:
                break;
            case StaticPropVersion.V4:
                break;
            case StaticPropVersion.V5:
                break;
            case StaticPropVersion.V6:
                break;
            case StaticPropVersion.V7s:
                break;
            case StaticPropVersion.V8:
                break;
            case StaticPropVersion.V9:
                break;
            case StaticPropVersion.V11:
                break;
            case StaticPropVersion.V12:
                break;
            default:
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
        {
            throw new NotImplementedException($"Unknown staticprop version (Version: {StaticProps.Version})");
        }
    }

    public override void Write(Stream stream)
    {
        var w = new BinaryWriter(stream);

        w.Write(StaticPropsDict.Data.Count);
        StaticPropsDict.Write(w.BaseStream);

        w.Write(StaticPropsLeaf.Data.Count);
        StaticPropsLeaf.Write(w.BaseStream);

        w.Write(StaticProps.Data.Count);
        StaticProps.Write(w.BaseStream);
    }

    public override bool Empty() => StaticPropsDict.Empty()
            && StaticPropsLeaf.Empty()
            && StaticProps.Empty();
}
