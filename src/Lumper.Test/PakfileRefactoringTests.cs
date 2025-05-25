namespace Lumper.Test;

using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Lumps.GameLumps;
using Lumper.Lib.Bsp.Struct;
using UpdateType = Lib.Bsp.Lumps.BspLumps.PakfileLump.PathReferenceUpdateType;

public class PakFileLumpRefactoringTests
{
    [Test]
    public void Entities_ExactMatch_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Entity }));
            Assert.That(entity.Properties[0], Is.TypeOf<Entity.EntityProperty<string>>());
            var prop = (Entity.EntityProperty<string>)entity.Properties[0];
            Assert.That(prop.Value, Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void Entities_WithoutDirectoryMatch_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("sound", "test/sound.wav"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "sound/test/sound.wav",
            "sound/test/renamed.wav",
            [UpdateType.Entity]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Entity }));
            Assert.That(entity.Properties[0], Is.TypeOf<Entity.EntityProperty<string>>());
            var prop = (Entity.EntityProperty<string>)entity.Properties[0];
            Assert.That(prop.Value, Is.EqualTo("test/renamed.wav"));
        });
    }

    [Test]
    public void Entities_NoMatches_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/different/example.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.That(changes, Is.Empty);
    }

    [Test]
    public void Entities_MultipleEntities_UpdatesAllMatches()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity1 = new Entity();
        entity1.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity1);

        var entity2 = new Entity();
        entity2.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity2);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Entity }));
            Assert.That(entity1.Properties[0], Is.TypeOf<Entity.EntityProperty<string>>());
            Assert.That(entity2.Properties[0], Is.TypeOf<Entity.EntityProperty<string>>());

            var prop1 = (Entity.EntityProperty<string>)entity1.Properties[0];
            var prop2 = (Entity.EntityProperty<string>)entity2.Properties[0];

            Assert.That(prop1.Value, Is.EqualTo("models/test/renamed.mdl"));
            Assert.That(prop2.Value, Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void Entities_NonStringProperty_DoesNotUpdate()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        _ = EntityIo.TryParse("a,b,c,0,1", out EntityIo? entityIo);
        entity.Properties.Add(new Entity.EntityProperty<EntityIo>("hello", entityIo!));
        entityLump.Data.Add(entity);

        List<UpdateType> result = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Entities_CrossDirectoryMove_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "materials/test/example.vtf",
            [UpdateType.Entity]
        );

        Assert.That(changes, Is.Empty);
        var prop = (Entity.EntityProperty<string>)entity.Properties[0];
        Assert.That(prop.Value, Is.EqualTo("models/test/example.mdl"), "Should not update cross-directory moves");
    }

    [Test]
    public void Entities_BackSlash_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", @"models\test\example.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Entity }));
            var prop = (Entity.EntityProperty<string>)entity.Properties[0];
            Assert.That(prop.Value, Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void Entities_MixedSlash_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", @"models\test/example.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Entity }));
            var prop = (Entity.EntityProperty<string>)entity.Properties[0];
            Assert.That(prop.Value, Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void Entities_CaseInsensitiveMatch_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "Models/Test/Example.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Entity }));
            var prop = (Entity.EntityProperty<string>)entity.Properties[0];
            Assert.That(prop.Value, Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void Entities_MultipleProperties_UpdatesAllMatches()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entity.Properties.Add(new Entity.EntityProperty<string>("skin", "other/path.vmt"));
        entity.Properties.Add(new Entity.EntityProperty<string>("material", "models/test/example.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Entity }));
            var prop1 = (Entity.EntityProperty<string>)entity.Properties[0];
            var prop2 = (Entity.EntityProperty<string>)entity.Properties[1];
            var prop3 = (Entity.EntityProperty<string>)entity.Properties[2];

            Assert.That(prop1.Value, Is.EqualTo("models/test/renamed.mdl"));
            Assert.That(prop2.Value, Is.EqualTo("other/path.vmt"), "Should not update non-matching paths");
            Assert.That(prop3.Value, Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void Entities_PartialPathMatch_DoesNotUpdate()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example_extra.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        Assert.That(changes, Is.Empty);
        var prop = (Entity.EntityProperty<string>)entity.Properties[0];
        Assert.That(prop.Value, Is.EqualTo("models/test/example_extra.mdl"), "Should not update partial path matches");
    }

    [Test]
    public void Entities_SoundPathWithoutPrefix_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("message", "ambient/water/drip1.wav"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "sound/ambient/water/drip1.wav",
            "sound/ambient/water/splash.wav",
            [UpdateType.Entity]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Entity }));
            var prop = (Entity.EntityProperty<string>)entity.Properties[0];
            Assert.That(prop.Value, Is.EqualTo("ambient/water/splash.wav"));
        });
    }

    [Test]
    public void Entities_NoSeparators_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("message", "filename.wav"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences("filename.wav", "renamed.wav", [UpdateType.Entity]);

        Assert.That(changes, Is.Empty);
        var prop = (Entity.EntityProperty<string>)entity.Properties[0];
        Assert.That(prop.Value, Is.EqualTo("filename.wav"), "Should not update paths without separators");
    }

    [Test]
    public void Pakfile_ExactMatch_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar"
            }
            """
        );
    }

    [Test]
    public void Pakfile_NoMatch_ReturnsEmpty()
    {
        TestPakfile(
            false,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/different"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/different"
            }
            """
        );
    }

    [Test]
    public void Pakfile_HandlesMultiplePathsInSameFile_UpdatesAllMatches()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo"
                "$bumpmap" "test/foo"
                "$phongexponent" "15"
                "$detail" "test/different"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar"
                "$bumpmap" "test/bar"
                "$phongexponent" "15"
                "$detail" "test/different"
            }
            """
        );
    }

    [Test]
    public void Pakfile_CaseInsensitiveMatching_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "TEST/FOO"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar"
            }
            """
        );
    }

    [Test]
    public void Pakfile_WithExtension_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo.vtf"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar.vtf"
            }
            """
        );
    }

    [Test]
    public void Pakfile_WithDirectory_ReturnsEmpty()
    {
        TestPakfile(
            false,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "materials/test/foo"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "materials/test/foo"
            }
            """
        );
    }

    [Test]
    public void Pakfile_WithDirectoryAndExtension_ReturnsEmpty()
    {
        TestPakfile(
            false,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "materials/test/foo.vtf"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "materials/test/foo.vtf"
            }
            """
        );
    }

    [Test]
    public void Pakfile_GenericTextFile_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            This is a regular text file that references test/foo.vtf
            and should be updated properly.
            """,
            """
            This is a regular text file that references test/bar.vtf
            and should be updated properly.
            """,
            "readme.txt"
        );
    }

    [Test]
    public void Pakfile_GenericTextFileWithoutExtension_ReturnsEmpty()
    {
        TestPakfile(
            false,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            This is a regular text file that references materials/test/foo
            and should be updated properly.
            """,
            """
            This is a regular text file that references materials/test/foo
            and should be updated properly.
            """,
            "readme.txt"
        );
    }

    [Test]
    public void Pakfile_VmtWithMissingQuotes_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            VertexLitGeneric
            {
                $basetexture test/foo
            }
            """,
            """
            VertexLitGeneric
            {
                $basetexture test/bar
            }
            """
        );
    }

    [Test]
    public void Pakfile_VmtWithMissingQuotesWithExtension_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            VertexLitGeneric
            {
                $basetexture test/foo.vtf
            }
            """,
            """
            VertexLitGeneric
            {
                $basetexture test/bar.vtf
            }
            """
        );
    }

    [Test]
    public void Pakfile_WorksIfContainsIgnorableExtension_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar"
            }
            """
        );
    }

    [Test]
    public void Pakfile_DifferentExtensionDoesntMatch_ReturnsEmpty()
    {
        TestPakfile(
            false,
            "materials/test/foo.mdl",
            "materials/test/bar.mdl",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo"
            }
            """
        );
    }

    [Test]
    public void Pakfile_PartialNameDoesntMatch_ReturnsEmpty()
    {
        TestPakfile(
            false,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foobaz"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foobaz"
            }
            """
        );
    }

    [Test]
    public void Pakfile_PartialNameNoQuotesDoesntMatch_ReturnsEmpty()
    {
        TestPakfile(
            false,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            VertexLitGeneric
            {
                $basetexture test/foobaz
            }
            """,
            """
            VertexLitGeneric
            {
                $basetexture test/foobaz
            }
            """
        );
    }

    [Test]
    public void Pakfile_MatchesBracedPath_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/{foo}.vtf",
            "materials/test/{bar}.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/{foo}"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/{bar}"
            }
            """
        );
    }

    [Test]
    public void Pakfile_MatchesBracedPathNoQuotes_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/{foo}.vtf",
            "materials/test/{bar}.vtf",
            """
            "VertexLitGeneric"
            {
                $basetexture test/{foo}
            }
            """,
            """
            "VertexLitGeneric"
            {
                $basetexture test/{bar}
            }
            """
        );
    }

    [Test]
    public void Pakfile_MatchesPathWithSpaces_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo baz.vtf",
            "materials/test/bar baz.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo baz"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar baz"
            }
            """
        );
    }

    // This is weird behaviour that we'd probably rather avoid, but requires a lot
    // more KV1 parsing to avoid, keeping this test for clarify.
    [Test]
    public void Pakfile_PathWithSpacesNoQuotes_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo bar.vtf",
            "materials/test/foo baz.vtf",
            """
            VertexLitGeneric
            {
                $basetexture test/foo bar
            }
            """,
            """
            VertexLitGeneric
            {
                $basetexture test/foo baz
            }
            """
        );
    }

    [Test]
    public void Pakfile_WithMultipleSpacesNoQuotes_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                $basetexture           test/foo
            }
            """,
            """
            "VertexLitGeneric"
            {
                $basetexture           test/bar
            }
            """
        );
    }

    [Test]
    public void Pakfile_NoQuotesWithDirectory_ReturnsEmpty()
    {
        TestPakfile(
            false,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                $basetexture materials/test/foo
            }
            """,
            """
            "VertexLitGeneric"
            {
                $basetexture materials/test/foo
            }
            """
        );
    }

    [Test]
    public void Pakfile_CRLF_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                $basetexture test/foo
            }
            """.Replace("\n", "\r\n"),
            """
            "VertexLitGeneric"
            {
                $basetexture test/bar
            }
            """.Replace("\n", "\r\n")
        );
    }

    [Test]
    public void Pakfile_NoQuotesCRLF_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                $basetexture test/foo
            }
            """.Replace("\n", "\r\n"),
            """
            "VertexLitGeneric"
            {
                $basetexture test/bar
            }
            """.Replace("\n", "\r\n")
        );
    }

    [Test]
    public void Pakfile_NoQuotesMultipleSpaces_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                $basetexture    test/foo
            }
            """,
            """
            "VertexLitGeneric"
            {
                $basetexture    test/bar
            }
            """
        );
    }

    [Test]
    public void Pakfile_NoQuotesTabChar_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                $basetexture	test/foo
            }
            """,
            """
            "VertexLitGeneric"
            {
                $basetexture	test/bar
            }
            """
        );
    }

    [Test]
    public void Pakfile_NoQuotesCombinedTabsAndSpaces_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                $basetexture  	 test/foo
            }
            """,
            """
            "VertexLitGeneric"
            {
                $basetexture  	 test/bar
            }
            """
        );
    }

    [Test]
    public void Pakfile_MaterialsMaterials_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/materials/test/foo.vtf",
            "materials/materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "materials/test/foo"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "materials/test/bar"
            }
            """
        );
    }

    [Test]
    public void Pakfile_CommentsInFile_PreservesComments()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            // This is a comment
            "VertexLitGeneric"
            {
                // Hello
                "$basetexture" "test/foo" // End of line comment
                "$bumpmap" "test/other"
            }
            """,
            """
            // This is a comment
            "VertexLitGeneric"
            {
                // Hello
                "$basetexture" "test/bar" // End of line comment
                "$bumpmap" "test/other"
            }
            """
        );
    }

    [Test]
    public void Pakfile_MatchBackslash_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test\foo"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar"
            }
            """
        );
    }

    [Test]
    public void Pakfile_MixedSeparators_UpdatesPath()
    {
        TestPakfile(
            true,
            "materials/test/foo/baz.vtf",
            "materials/test/bar/baz.vtf",
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test\foo/baz"
            }
            """,
            """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar/baz"
            }
            """
        );
    }

    private static void TestPakfile(
        bool shouldChange,
        string oldPath,
        string newPath,
        string inData,
        string outData,
        string fileName = "test.vmt",
        string[]? limitPakfileExtensions = null
    )
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, fileName, inData);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            oldPath,
            newPath,
            [UpdateType.Pakfile],
            limitPakfileExtensions
        );

        ReadOnlySpan<byte> data = pakfileLump.Entries[0].GetData();
        string str = BspFile.Encoding.GetString(data);
        Assert.Multiple(() =>
        {
            Assert.That(str, Is.EqualTo(outData));
            Assert.That(changes, shouldChange ? Is.EqualTo(new List<UpdateType> { UpdateType.Pakfile }) : Is.Empty);
        });
    }

    [Test]
    public void Pakfile_MultiplePakfileEntries_UpdatesAllMatches()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        const string content1 = """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo"
            }
            """;

        const string content2 = """
            "VertexLitGeneric"
            {
                "$basetexture" "test/foo"
            }
            """;

        const string content3 = """
            "VertexLitGeneric"
            {
                "$basetexture" "test/different"
            }
            """;

        TestUtils.AddPakfileEntry(pakfileLump, "test1.vmt", content1);
        TestUtils.AddPakfileEntry(pakfileLump, "test2.vmt", content2);
        TestUtils.AddPakfileEntry(pakfileLump, "test3.vmt", content3);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            [UpdateType.Pakfile]
        );

        const string expected = """
            "VertexLitGeneric"
            {
                "$basetexture" "test/bar"
            }
            """;

        Assert.Multiple(() =>
        {
            Assert.That(BspFile.Encoding.GetString(pakfileLump.Entries[0].GetData()), Is.EqualTo(expected));
            Assert.That(BspFile.Encoding.GetString(pakfileLump.Entries[1].GetData()), Is.EqualTo(expected));
            Assert.That(BspFile.Encoding.GetString(pakfileLump.Entries[2].GetData()), Is.EqualTo(content3));
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.Pakfile }));
        });
    }

    [Test]
    public void TexData_ExactMatch_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "test/example" };
        texDataLump.Data.Add(texData);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.TexData }));
            Assert.That(texData.TexName, Is.EqualTo("test/renamed"));
        });
    }

    [Test]
    public void TexData_NoMatch_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "test/different" };
        texDataLump.Data.Add(texData);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(texData.TexName, Is.EqualTo("test/different"));
        });
    }

    [Test]
    public void TexData_CaseInsensitiveMatch_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "TEST/EXAMPLE" };
        texDataLump.Data.Add(texData);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.TexData }));
            Assert.That(texData.TexName, Is.EqualTo("test/renamed"));
        });
    }

    [Test]
    public void TexData_NonVmtExtension_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "test/example" };
        texDataLump.Data.Add(texData);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(texData.TexName, Is.EqualTo("test/example"));
        });
    }

    [Test]
    public void TexData_NonMaterialsPrefix_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "test/example" };
        texDataLump.Data.Add(texData);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.vmt",
            "models/test/renamed.vmt",
            [UpdateType.TexData]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(texData.TexName, Is.EqualTo("test/example"));
        });
    }

    [Test]
    public void TexData_MultipleMatches_UpdatesAllPaths()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData1 = new TexData { TexName = "test/example" };
        var texData2 = new TexData { TexName = "test/example" };
        var texData3 = new TexData { TexName = "test/different" };
        texDataLump.Data.Add(texData1);
        texDataLump.Data.Add(texData2);
        texDataLump.Data.Add(texData3);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.TexData }));
            Assert.That(texData1.TexName, Is.EqualTo("test/renamed"));
            Assert.That(texData2.TexName, Is.EqualTo("test/renamed"));
            Assert.That(texData3.TexName, Is.EqualTo("test/different"));
        });
    }

    [Test]
    public void TexData_UpdatesPathAlongWithOtherTypes()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        // Add TexData entry
        var texData = new TexData { TexName = "test/example" };
        texDataLump.Data.Add(texData);

        // Add Entity entry referencing the same material
        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("material", "materials/test/example.vmt"));
        entityLump.Data.Add(entity);

        // Add Pakfile entry
        pakfileLump.Entries.Add(
            new PakfileEntry(
                pakfileLump,
                "test.vmt",
                new MemoryStream(
                    BspFile.Encoding.GetBytes(
                        """
                        "VertexLitGeneric"
                        {
                            "$basetexture" "test/example"
                        }
                        """
                    )
                )
            )
        );

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt"
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Contains.Item(UpdateType.TexData));
            Assert.That(changes, Contains.Item(UpdateType.Entity));
            Assert.That(changes, Contains.Item(UpdateType.Pakfile));
            Assert.That(texData.TexName, Is.EqualTo("test/renamed"));

            var prop = (Entity.EntityProperty<string>)entity.Properties[0];
            Assert.That(prop.Value, Is.EqualTo("materials/test/renamed.vmt"));

            string pakContent = BspFile.Encoding.GetString(pakfileLump.Entries[0].GetData());

            Assert.That(
                pakContent,
                Is.EqualTo(
                    """
                    "VertexLitGeneric"
                    {
                        "$basetexture" "test/renamed"
                    }
                    """
                )
            );
        });
    }

    [Test]
    public void StaticProp_ExactMatch_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add("models/test/example.mdl");

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.StaticProp]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.StaticProp }));
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void StaticProp_NoMatch_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add("models/test/different.mdl");

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.StaticProp]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo("models/test/different.mdl"));
        });
    }

    [Test]
    public void StaticProp_CaseInsensitiveMatch_UpdatesPath()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add("MODELS/TEST/EXAMPLE.MDL");

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.StaticProp]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.StaticProp }));
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void StaticProp_NonMdlExtension_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add("models/test/example.mdl");

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.vmt",
            "models/test/renamed.vmt",
            [UpdateType.StaticProp]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo("models/test/example.mdl"));
        });
    }

    [Test]
    public void StaticProp_NonModelsPrefix_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add("models/test/example.mdl");

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.mdl",
            "materials/test/renamed.mdl",
            [UpdateType.StaticProp]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo("models/test/example.mdl"));
        });
    }

    // The PakfileLump.UpdateStaticPropPathReferences method only updates the first match it finds.
    // This is a dictionary, Source should only ever include one entry for each model, so intended behaviour.
    [Test]
    public void StaticProp_MultipleModels_UpdatesMatchingOne()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add("models/test/example.mdl");
        sprpLump.StaticPropsDict.Data.Add("models/test/different.mdl");
        sprpLump.StaticPropsDict.Data.Add("models/test/example.mdl"); // Duplicate intentionally

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.StaticProp]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.StaticProp }));
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo("models/test/renamed.mdl"));
            Assert.That(sprpLump.StaticPropsDict.Data[1], Is.EqualTo("models/test/different.mdl"));
            Assert.That(sprpLump.StaticPropsDict.Data[2], Is.EqualTo("models/test/example.mdl"));
        });
    }

    // Backslashes should never be valid, either in our paths, or in the StaticPropDict vbsp produces.
    [Test]
    public void StaticProp_BackslashInPath_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add(@"models\test\example.mdl");

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.StaticProp]
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo(@"models\test\example.mdl"));
        });
    }

    [Test]
    public void StaticProp_NullStaticPropDict_ReturnsEmpty()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict = null;

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.StaticProp]
        );

        Assert.That(changes, Is.Empty);
    }

    [Test]
    public void StaticProp_UpdatesPathAlongWithOtherTypes()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        // Add StaticProp entry
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add("models/test/example.mdl");

        // Add Entity referencing the same model
        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity);

        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl"
        );

        Assert.Multiple(() =>
        {
            Assert.That(changes, Contains.Item(UpdateType.StaticProp));
            Assert.That(changes, Contains.Item(UpdateType.Entity));
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo("models/test/renamed.mdl"));

            var prop = (Entity.EntityProperty<string>)entity.Properties[0];
            Assert.That(prop.Value, Is.EqualTo("models/test/renamed.mdl"));
        });
    }

    [Test]
    public void ProcessMapRename_MaterialsMapsPath_RenamesFile()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, "materials/maps/surf_utopia/cubemapdefault.vmt");
        pakfileLump.IsModified = false; // Creating new PakFileEntry from stream sets IsModified to true, so we reset it.

        pakfileLump.ProcessMapRename("surf_utopia", "surf_newname");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("materials/maps/surf_newname/cubemapdefault.vmt"));
            Assert.That(pakfileLump.IsModified, Is.True);
        });
    }

    [Test]
    public void ProcessMapRename_SoundscapesTxtFile_RenamesFile()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, "scripts/soundscapes_kz_example.txt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("kz_example", "kz_renamed");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("scripts/soundscapes_kz_renamed.txt"));
            Assert.That(pakfileLump.IsModified, Is.True);
        });
    }

    [Test]
    public void ProcessMapRename_SoundscapesVscFile_RenamesFile()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, "scripts/soundscapes_bhop_forest.vsc");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("bhop_forest", "bhop_jungle");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("scripts/soundscapes_bhop_jungle.vsc"));
            Assert.That(pakfileLump.IsModified, Is.True);
        });
    }

    [Test]
    public void ProcessMapRename_LevelSoundsFile_RenamesFile()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, "maps/surf_mesa_level_sounds.txt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("surf_mesa", "surf_plateau");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("maps/surf_plateau_level_sounds.txt"));
            Assert.That(pakfileLump.IsModified, Is.True);
        });
    }

    [Test]
    public void ProcessMapRename_ParticlesFile_RenamesFile()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, "maps/kz_climb_particles.txt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("kz_climb", "kz_ascent");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("maps/kz_ascent_particles.txt"));
            Assert.That(pakfileLump.IsModified, Is.True);
        });
    }

    [Test]
    public void ProcessMapRename_MultipleDifferentFiles_RenamesAllMatchingFiles()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        // Add multiple different types of files
        TestUtils.AddPakfileEntry(pakfileLump, "materials/maps/surf_y/texture.vmt");
        TestUtils.AddPakfileEntry(pakfileLump, "scripts/soundscapes_surf_y.txt");
        TestUtils.AddPakfileEntry(pakfileLump, "maps/surf_y_level_sounds.txt");
        TestUtils.AddPakfileEntry(pakfileLump, "maps/surf_y_particles.txt");
        TestUtils.AddPakfileEntry(pakfileLump, "materials/models/props/barrel.vmt"); // Shouldn't be changed
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("surf_y", "surf_mastery");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("materials/maps/surf_mastery/texture.vmt"));
            Assert.That(pakfileLump.Entries[1].Key, Is.EqualTo("scripts/soundscapes_surf_mastery.txt"));
            Assert.That(pakfileLump.Entries[2].Key, Is.EqualTo("maps/surf_mastery_level_sounds.txt"));
            Assert.That(pakfileLump.Entries[3].Key, Is.EqualTo("maps/surf_mastery_particles.txt"));
            Assert.That(pakfileLump.Entries[4].Key, Is.EqualTo("materials/models/props/barrel.vmt"));
            Assert.That(pakfileLump.IsModified, Is.True);
        });
    }

    [Test]
    public void ProcessMapRename_CaseInsensitiveMapName_RenamesFile()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        // Add pakfile entry with differently cased map name
        TestUtils.AddPakfileEntry(pakfileLump, "materials/maps/SURF_UTOPIA/texture.vmt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("surf_utopia", "surf_paradise");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("materials/maps/surf_paradise/texture.vmt"));
            Assert.That(pakfileLump.IsModified, Is.True);
        });
    }

    [Test]
    public void ProcessMapRename_NoMatchingFiles_DoesNothing()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, "materials/models/props/barrel.vmt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("surf_utopia", "surf_paradise");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("materials/models/props/barrel.vmt"));
            Assert.That(pakfileLump.IsModified, Is.False);
        });
    }

    [Test]
    public void ProcessMapRename_MaterialsWithSubfolders_RenamesFile()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, "materials/maps/surf_utopia/subfolder/texture.vmt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("surf_utopia", "surf_paradise");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("materials/maps/surf_paradise/subfolder/texture.vmt"));
            Assert.That(pakfileLump.IsModified, Is.True);
        });
    }

    [Test]
    public void ProcessMapRename_PartialMapNameMatch_DoesNotRename()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        // Add entry with a name that contains the map name as a substring
        TestUtils.AddPakfileEntry(pakfileLump, "materials/maps/surf_utopia2/texture.vmt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("surf_utopia", "surf_paradise");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("materials/maps/surf_utopia2/texture.vmt"));
            Assert.That(pakfileLump.IsModified, Is.False);
        });
    }

    [Test]
    public void ProcessMapRename_DifferentMapName_DoesNotRename()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        TestUtils.AddPakfileEntry(pakfileLump, "materials/maps/surf_different/texture.vmt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("surf_utopia", "surf_paradise");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("materials/maps/surf_different/texture.vmt"));
            Assert.That(pakfileLump.IsModified, Is.False);
        });
    }

    [Test]
    public void ProcessMapRename_SimilarButDifferentPaths_DoesNotRename()
    {
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        // Add entries with similar but different paths
        TestUtils.AddPakfileEntry(pakfileLump, "materials/maps_custom/surf_utopia/texture.vmt");
        TestUtils.AddPakfileEntry(pakfileLump, "scripts/soundscape_surf_utopia.txt");
        TestUtils.AddPakfileEntry(pakfileLump, "maps/surf_utopia_particles_custom.txt");
        pakfileLump.IsModified = false;

        pakfileLump.ProcessMapRename("surf_utopia", "surf_paradise");

        Assert.Multiple(() =>
        {
            Assert.That(pakfileLump.Entries[0].Key, Is.EqualTo("materials/maps_custom/surf_utopia/texture.vmt"));
            Assert.That(pakfileLump.Entries[1].Key, Is.EqualTo("scripts/soundscape_surf_utopia.txt"));
            Assert.That(pakfileLump.Entries[2].Key, Is.EqualTo("maps/surf_utopia_particles_custom.txt"));
            Assert.That(pakfileLump.IsModified, Is.False);
        });
    }
}
