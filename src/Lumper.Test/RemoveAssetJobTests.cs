namespace Lumper.Test;

using Lumper.Lib.AssetManifest;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Jobs;

[TestFixture]
public class RemoveAssetJobTests
{
    private BspFile _bspFile;
    private PakfileLump _pakfileLump;
    private Dictionary<string, List<AssetManifest.Asset>> _manifest = null!;
    private static readonly List<string> Origins = ["hl2", "css", "portal2", "csgo"];

    [SetUp]
    public void Setup()
    {
        // Create mock BSP file
        _bspFile = TestUtils.CreateMockBspFile();
        _pakfileLump = _bspFile.GetLump<PakfileLump>();

        // Clear any existing entries
        _pakfileLump.Entries.Clear();

        // Setup manifest for testing
        _manifest = TestUtils.GetMutableAssetManifest();
    }

    [TearDown]
    public void TearDown()
    {
        _bspFile.Dispose();
    }

    [Test]
    public void Run_HashMatchingEntry_IsRemoved()
    {
        _manifest["hash1"] = [new AssetManifest.Asset { Origin = "hl2", Path = "materials/hl2/texture.vtf" }];
        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/hl2/texture.vtf");

        var job = new RemoveAssetJob { OriginFilter = Origins };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_pakfileLump.Entries, Is.Empty);
        });
    }

    [Test]
    public void Run_HashMatchingEntry_UpdatesPathReferences()
    {
        _manifest["hash1"] = [new AssetManifest.Asset { Origin = "hl2", Path = "materials/hl2/texture.vtf" }];
        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/my_map/some_idiot_renamed_a_hl2_file.vtf");
        TestUtils.AddPakfileEntry(
            _pakfileLump,
            "materials/whatever.vmt",
            """Material" { "$basetexture" "my_map/some_idiot_renamed_a_hl2_file" }"""
        );

        var job = new RemoveAssetJob { OriginFilter = Origins };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_pakfileLump.Entries, Has.Count.EqualTo(1));
            Assert.That(
                BspFile.Encoding.GetString(_pakfileLump.Entries[0].GetData()),
                Is.EqualTo("""Material" { "$basetexture" "hl2/texture" }""")
            );
        });
    }

    [Test]
    public void Run_MultipleOriginsWithPriority_PicksHighestPriority()
    {
        // Add two assets with the same hash but different origins
        _manifest["hash1"] =
        [
            new AssetManifest.Asset { Origin = "csgo", Path = "materials/csgo/texture.vtf" },
            new AssetManifest.Asset { Origin = "hl2", Path = "materials/hl2/texture.vtf" },
        ];

        // Add a pakfile entry with that hash but using a custom path
        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/my_map/custom_texture.vtf");
        TestUtils.AddPakfileEntry(
            _pakfileLump,
            "materials/custom.vmt",
            """Material" { "$basetexture" "my_map/custom_texture" }"""
        );

        var job = new RemoveAssetJob { OriginFilter = Origins };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_pakfileLump.Entries, Has.Count.EqualTo(1));
            // Should update to hl2 path as it has higher priority than csgo
            Assert.That(
                BspFile.Encoding.GetString(_pakfileLump.Entries[0].GetData()),
                Is.EqualTo("""Material" { "$basetexture" "hl2/texture" }""")
            );
        });
    }

    [Test]
    public void Run_MultipleOriginsNoneInPriority_UsesFirstAsset()
    {
        // Add two assets with the same hash but from origins not in priority list
        _manifest["hash1"] =
        [
            new AssetManifest.Asset { Origin = "gmod", Path = "materials/gmod/texture.vtf" },
            new AssetManifest.Asset { Origin = "l4d2", Path = "materials/l4d2/texture.vtf" },
        ];

        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/my_map/custom_texture.vtf");
        TestUtils.AddPakfileEntry(
            _pakfileLump,
            "materials/custom.vmt",
            """Material" { "$basetexture" "my_map/custom_texture" }"""
        );

        var job = new RemoveAssetJob { OriginFilter = ["gmod", "l4d2"] };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_pakfileLump.Entries, Has.Count.EqualTo(1));
            // Should use the first asset (gmod) since none are in priority list
            Assert.That(
                BspFile.Encoding.GetString(_pakfileLump.Entries[0].GetData()),
                Is.EqualTo("""Material" { "$basetexture" "gmod/texture" }""")
            );
        });
    }

    [Test]
    public void Run_PriorityMatchIsNotInOriginFilter_StillRemovesAsset()
    {
        // Add assets with the same hash, one in priority list but not in filter
        _manifest["hash1"] =
        [
            new AssetManifest.Asset { Origin = "hl2", Path = "materials/hl2/texture.vtf" },
            new AssetManifest.Asset { Origin = "gmod", Path = "materials/gmod/texture.vtf" },
        ];

        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/my_map/custom_texture.vtf");

        // Only filter for gmod
        var job = new RemoveAssetJob { OriginFilter = ["gmod"] };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_pakfileLump.Entries, Is.Empty);
        });
    }

    [Test]
    public void Run_MultipleMatchingOriginsWithPathMatch_UsesExactMatch()
    {
        // Add multiple assets with same hash, including one with exact path match
        _manifest["hash1"] =
        [
            new AssetManifest.Asset { Origin = "csgo", Path = "materials/csgo/texture.vtf" },
            new AssetManifest.Asset { Origin = "css", Path = "materials/css/texture.vtf" },
        ];

        // The entry has exact path match with css origin
        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/css/texture.vtf");
        TestUtils.AddPakfileEntry(
            _pakfileLump,
            "materials/custom.vmt",
            """Material" { "$basetexture" "css/texture" }"""
        );

        var job = new RemoveAssetJob { OriginFilter = Origins };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_pakfileLump.Entries, Has.Count.EqualTo(1));
            // No actual change should happen since the paths match
            Assert.That(
                BspFile.Encoding.GetString(_pakfileLump.Entries[0].GetData()),
                Is.EqualTo("""Material" { "$basetexture" "css/texture" }""")
            );
        });
    }

    [Test]
    public void Run_NoMatchingHash_ReturnsFalse()
    {
        // Add an entry with a hash that doesn't exist in the manifest
        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "nonexistentHash", "materials/custom/texture.vtf");

        var job = new RemoveAssetJob { OriginFilter = Origins };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(_pakfileLump.Entries, Has.Count.EqualTo(1), "Entry should not be removed");
        });
    }

    [Test]
    public void Run_EmptyPakfileLump_ReturnsFalse()
    {
        // PakfileLump is already empty from Setup()
        var job = new RemoveAssetJob { OriginFilter = Origins };
        bool result = job.Run(_bspFile);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Run_MultipleEntriesRemoved_ReturnsTrue()
    {
        // Add multiple entries that match assets in the manifest
        _manifest["hash1"] = [new AssetManifest.Asset { Origin = "hl2", Path = "materials/hl2/texture1.vtf" }];
        _manifest["hash2"] = [new AssetManifest.Asset { Origin = "css", Path = "materials/css/texture2.vtf" }];

        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/hl2/texture1.vtf");
        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash2", "materials/css/texture2.vtf");

        var job = new RemoveAssetJob { OriginFilter = Origins };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_pakfileLump.Entries, Is.Empty, "All entries should be removed");
        });
    }

    [Test]
    public void Run_HashMatchesButOriginNotInFilter_ReturnsFalse()
    {
        // Add an asset with origin not in the filter
        _manifest["hash1"] = [new AssetManifest.Asset { Origin = "portal", Path = "materials/portal/texture.vtf" }];
        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/portal/texture.vtf");

        // Filter only includes hl2, css, etc, but not portal
        var job = new RemoveAssetJob { OriginFilter = Origins };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(_pakfileLump.Entries, Has.Count.EqualTo(1), "Entry should not be removed");
        });
    }

    [Test]
    public void Run_NullOriginFilter_RemovesAllMatchingAssets()
    {
        // Add assets from various origins
        _manifest["hash1"] = [new AssetManifest.Asset { Origin = "hl2", Path = "materials/hl2/texture.vtf" }];
        _manifest["hash2"] = [new AssetManifest.Asset { Origin = "portal", Path = "materials/portal/texture.vtf" }];

        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash1", "materials/hl2/texture.vtf");
        TestUtils.AddPakfileEntryWithHash(_pakfileLump, "hash2", "materials/portal/texture.vtf");

        // Null filter should remove all matching assets regardless of origin
        var job = new RemoveAssetJob { OriginFilter = null };
        bool result = job.Run(_bspFile);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(_pakfileLump.Entries, Is.Empty, "All entries should be removed with null filter");
        });
    }
}
