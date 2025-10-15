namespace Lumper.Test;

using Lib.Bsp.Enum;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Jobs;

[TestFixture]
public class AddSkyboxOcclusionFlagTests
{
    private BspFile _bspFile;
    private TexInfoLump _texinfoLump;

    [SetUp]
    public void Setup()
    {
        _bspFile = TestUtils.CreateMockBspFile();
        _texinfoLump = _bspFile.GetLump<TexInfoLump>()!;

        _texinfoLump.Data.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        _bspFile.Dispose();
    }

    [Test]
    public void TestModifySkyMaterial()
    {
        _texinfoLump.Data.Add(new() { Flags = SurfaceFlag.Sky });

        var job = new AddSkyOcclusionFlagJob();
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_texinfoLump.Data[0].Flags, Is.EqualTo(SurfaceFlag.Sky | SurfaceFlag.SkyOcclusion));
        });
    }

    [Test]
    public void TestModify2DSkyMaterial()
    {
        _texinfoLump.Data.Add(new() { Flags = SurfaceFlag.Sky2d });

        var job = new AddSkyOcclusionFlagJob();
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_texinfoLump.Data[0].Flags, Is.EqualTo(SurfaceFlag.Sky2d | SurfaceFlag.SkyOcclusion));
        });
    }

    [Test]
    public void TestNoChangeToNonSkyMaterial()
    {
        _texinfoLump.Data.Add(new() { Flags = SurfaceFlag.None });

        var job = new AddSkyOcclusionFlagJob();
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(_texinfoLump.Data[0].Flags, Is.EqualTo(SurfaceFlag.None));
        });
    }
}
