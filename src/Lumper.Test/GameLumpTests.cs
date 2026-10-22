namespace Lumper.Test;

using System.IO.Compression;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using Lumper.Lib.Bsp.Lumps.BspLumps;

[TestFixture]
public class GameLumpTests
{
    private const string PakfileEntryPath = "materials/foo/bar.vmt";

    // BSP with a gamelump of the given length (0 or a 4-byte header with no lumps in it), then a pakfile
    // and another lump after it, laid out like BSPConvert's GoldSrc and Quake 3 ports.
    private static MemoryStream CreateBspWithEmptyGameLump(int gameLumpLength)
    {
        using var zipStream = new MemoryStream();
        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            using var entryWriter = new StreamWriter(zip.CreateEntry(PakfileEntryPath).Open());
            entryWriter.Write("\"LightmappedGeneric\" {}");
        }

        byte[] zipBytes = zipStream.ToArray();

        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, BspFile.Encoding, true);

        writer.Write("VBSP"u8.ToArray());
        writer.Write(25);

        const int gameLumpOffset = BspFile.HeaderSize;
        int pakfileOffset = gameLumpOffset + gameLumpLength;
        int leafAmbientLightingOffset = pakfileOffset + ((zipBytes.Length + 3) & ~3);
        for (int i = 0; i < BspFile.HeaderLumps; i++)
        {
            (int offset, int length) = (BspLumpType)i switch
            {
                BspLumpType.GameLump => (gameLumpOffset, gameLumpLength),
                BspLumpType.Pakfile => (pakfileOffset, zipBytes.Length),
                BspLumpType.LeafAmbientLighting => (leafAmbientLightingOffset, 4),
                _ => (0, 0),
            };

            writer.Write(offset);
            writer.Write(length);
            writer.Write(0); // Version
            writer.Write(0); // FourCC
        }

        writer.Write(0); // Revision

        if (gameLumpLength > 0)
            writer.Write(0); // Gamelump count

        writer.Write(zipBytes);
        stream.Position = leafAmbientLightingOffset;
        writer.Write(0);

        stream.Position = 0;
        return stream;
    }

    private static (int Offset, int Length) ReadLumpHeader(Stream stream, BspLumpType type)
    {
        using var reader = new BinaryReader(stream, BspFile.Encoding, true);
        stream.Position = 8 + ((int)type * 16);
        return (reader.ReadInt32(), reader.ReadInt32());
    }

    [TestCase(DesiredCompression.Uncompressed)]
    [TestCase(DesiredCompression.Compressed)]
    public void TestEmptyGameLumpKeepsHeader(DesiredCompression compression)
    {
        using MemoryStream input = CreateBspWithEmptyGameLump(4);
        using BspFile bsp = BspFile.FromStream(input, null)!;

        using var output = new MemoryStream();
        Assert.That(bsp.SaveToStream(new IoHandler(new CancellationTokenSource()), output, compression), Is.True);

        (int gameLumpOffset, int gameLumpLength) = ReadLumpHeader(output, BspLumpType.GameLump);
        (int pakfileOffset, _) = ReadLumpHeader(output, BspLumpType.Pakfile);

        Assert.Multiple(() =>
        {
            Assert.That(gameLumpLength, Is.EqualTo(4));
            Assert.That(pakfileOffset, Is.Not.EqualTo(gameLumpOffset));
        });

        output.Position = 0;
        using BspFile reloaded = BspFile.FromStream(output, null)!;
        Assert.That(reloaded.GetLump<PakfileLump>()!.Entries.Select(x => x.Key), Is.EqualTo([PakfileEntryPath]));
    }

    // Older Lumper versions wrote empty gamelumps with a length of 0, at the same offset as the lump after them.
    [Test]
    public void TestZeroLengthGameLumpAtNextLumpsOffset()
    {
        using MemoryStream input = CreateBspWithEmptyGameLump(0);
        using BspFile bsp = BspFile.FromStream(input, null)!;

        Assert.Multiple(() =>
        {
            Assert.That(bsp.GetLump<GameLump>()!.Lumps, Is.Empty);
            Assert.That(bsp.GetLump<PakfileLump>()!.Entries.Select(x => x.Key), Is.EqualTo([PakfileEntryPath]));
        });
    }
}
