using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using UpdateType = Lumper.Lib.Bsp.Lumps.BspLumps.PakfileLump.PathReferenceUpdateType;

namespace Lumper.Test;

using Lib.Bsp.Lumps.GameLumps;

public class PakFileLumpRefactoringTests
{
    [Test]
    public void Entities_ExactMatch_UpdatesPath()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("sound", "test/sound.wav"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "sound/test/sound.wav",
            "sound/test/renamed.wav",
            [UpdateType.Entity]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/different/example.mdl"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
        Assert.That(changes, Is.Empty);
    }

    [Test]
    public void Entities_MultipleEntities_UpdatesAllMatches()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity1 = new Entity();
        entity1.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity1);

        var entity2 = new Entity();
        entity2.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity2);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<EntityIo>("hello", new EntityIo("a,b,c,0,1", ',')));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> result = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Entities_CrossDirectoryMove_ReturnsEmpty()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "materials/test/example.vtf",
            [UpdateType.Entity]
        );

        // Assert
        Assert.That(changes, Is.Empty);
        var prop = (Entity.EntityProperty<string>)entity.Properties[0];
        Assert.That(prop.Value, Is.EqualTo("models/test/example.mdl"), "Should not update cross-directory moves");
    }

    [Test]
    public void Entities_BackSlash_UpdatesPath()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", @"models\test\example.mdl"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", @"models\test/example.mdl"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "Models/Test/Example.mdl"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example.mdl"));
        entity.Properties.Add(new Entity.EntityProperty<string>("skin", "other/path.vmt"));
        entity.Properties.Add(new Entity.EntityProperty<string>("material", "models/test/example.mdl"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", "models/test/example_extra.mdl"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.Entity]
        );

        // Assert
        Assert.That(changes, Is.Empty);
        var prop = (Entity.EntityProperty<string>)entity.Properties[0];
        Assert.That(prop.Value, Is.EqualTo("models/test/example_extra.mdl"), "Should not update partial path matches");
    }

    [Test]
    public void Entities_SoundPathWithoutPrefix_UpdatesPath()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("message", "ambient/water/drip1.wav"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "sound/ambient/water/drip1.wav",
            "sound/ambient/water/splash.wav",
            [UpdateType.Entity]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("message", "filename.wav"));
        entityLump.Data.Add(entity);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences("filename.wav", "renamed.wav", [UpdateType.Entity]);

        // Assert
        Assert.That(changes, Is.Empty);
        var prop = (Entity.EntityProperty<string>)entity.Properties[0];
        Assert.That(prop.Value, Is.EqualTo("filename.wav"), "Should not update paths without separators");
    }

    [Test]
    public void Entities_NullPropertyValue_DoesNotCrash()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        EntityLump entityLump = bspFile.GetLump<EntityLump>();

        var entity = new Entity();
        entity.Properties.Add(new Entity.EntityProperty<string>("model", null));
        entityLump.Data.Add(entity);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            List<UpdateType> changes = pakfileLump.UpdatePathReferences(
                "models/test/example.mdl",
                "models/test/renamed.mdl",
                [UpdateType.Entity]
            );

            Assert.That(changes, Is.Empty);
        });
    }

    [Test]
    public void Pakfile_ExactMatch_UpdatesPath() =>
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

    [Test]
    public void Pakfile_NoMatch_ReturnsEmpty() =>
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

    [Test]
    public void Pakfile_HandlesMultiplePathsInSameFile_UpdatesAllMatches() =>
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

    [Test]
    public void Pakfile_CaseInsensitiveMatching_UpdatesPath() =>
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

    [Test]
    public void Pakfile_WithExtension_UpdatesPath() =>
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

    [Test]
    public void Pakfile_WithDirectory_ReturnsEmpty() =>
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

    [Test]
    public void Pakfile_WithDirectoryAndExtension_ReturnsEmpty() =>
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

    [Test]
    public void Pakfile_GenericTextFile_UpdatesPath() =>
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

    [Test]
    public void Pakfile_GenericTextFileWithoutExtension_ReturnsEmpty() =>
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

    [Test]
    public void Pakfile_VmtWithMissingQuotes_UpdatesPath() =>
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

    [Test]
    public void Pakfile_VmtWithMissingQuotesWithExtension_UpdatesPath() =>
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

    [Test]
    public void Pakfile_WorksIfContainsIgnorableExtension_UpdatesPath() =>
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

    [Test]
    public void Pakfile_DifferentExtensionDoesntMatch_ReturnsEmpty() =>
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

    [Test]
    public void Pakfile_PartialNameDoesntMatch_ReturnsEmpty() =>
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

    [Test]
    public void Pakfile_PartialNameNoQuotesDoesntMatch_ReturnsEmpty() =>
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

    [Test]
    public void Pakfile_MatchesBracedPath_UpdatesPath() =>
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

    [Test]
    public void Pakfile_MatchesBracedPathNoQuotes_UpdatesPath() =>
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

    [Test]
    public void Pakfile_MatchesPathWithSpaces_UpdatesPath() =>
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

    // This is weird behaviour that we'd probably rather avoid, but requires a lot
    // more KV1 parsing to avoid, keeping this test for clarify.
    [Test]
    public void Pakfile_PathWithSpacesNoQuotes_UpdatesPath() =>
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

    [Test]
    public void Pakfile_WithMultipleSpacesNoQuotes_UpdatesPath() =>
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

    [Test]
    public void Pakfile_NoQuotesWithDirectory_ReturnsEmpty() =>
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

    [Test]
    public void Pakfile_CRLF_UpdatesPath() =>
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

    [Test]
    public void Pakfile_NoQuotesCRLF_UpdatesPath() =>
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

    [Test]
    public void Pakfile_NoQuotesMultipleSpaces_UpdatesPath() =>
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

    [Test]
    public void Pakfile_NoQuotesTabChar_UpdatesPath() =>
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

    [Test]
    public void Pakfile_NoQuotesCombinedTabsAndSpaces_UpdatesPath() =>
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

    [Test]
    public void Pakfile_MaterialsMaterials_UpdatesPath() =>
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

    [Test]
    public void Pakfile_CommentsInFile_PreservesComments() =>
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        pakfileLump.Entries.Add(
            new PakfileEntry(pakfileLump, fileName, new MemoryStream(BspFile.Encoding.GetBytes(inData)))
        );

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            oldPath,
            newPath,
            [UpdateType.Pakfile],
            limitPakfileExtensions
        );

        // Assert
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
        // Arrange
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

        pakfileLump.Entries.Add(
            new PakfileEntry(pakfileLump, "test1.vmt", new MemoryStream(BspFile.Encoding.GetBytes(content1)))
        );
        pakfileLump.Entries.Add(
            new PakfileEntry(pakfileLump, "test2.vmt", new MemoryStream(BspFile.Encoding.GetBytes(content2)))
        );
        pakfileLump.Entries.Add(
            new PakfileEntry(pakfileLump, "test3.vmt", new MemoryStream(BspFile.Encoding.GetBytes(content3)))
        );

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/foo.vtf",
            "materials/test/bar.vtf",
            [UpdateType.Pakfile]
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "test/example" };
        texDataLump.Data.Add(texData);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.TexData }));
            Assert.That(texData.TexName, Is.EqualTo("test/renamed"));
        });
    }

    [Test]
    public void TexData_NoMatch_ReturnsEmpty()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "test/different" };
        texDataLump.Data.Add(texData);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(texData.TexName, Is.EqualTo("test/different"));
        });
    }

    [Test]
    public void TexData_CaseInsensitiveMatch_UpdatesPath()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "TEST/EXAMPLE" };
        texDataLump.Data.Add(texData);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.TexData }));
            Assert.That(texData.TexName, Is.EqualTo("test/renamed"));
        });
    }

    [Test]
    public void TexData_NonVmtExtension_ReturnsEmpty()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "test/example" };
        texDataLump.Data.Add(texData);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vtf",
            "materials/test/renamed.vtf",
            [UpdateType.TexData]
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(texData.TexName, Is.EqualTo("test/example"));
        });
    }

    [Test]
    public void TexData_NonMaterialsPrefix_ReturnsEmpty()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData = new TexData { TexName = "test/example" };
        texDataLump.Data.Add(texData);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.vmt",
            "models/test/renamed.vmt",
            [UpdateType.TexData]
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Empty);
            Assert.That(texData.TexName, Is.EqualTo("test/example"));
        });
    }

    [Test]
    public void TexData_MultipleMatches_UpdatesAllPaths()
    {
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        TexDataLump texDataLump = bspFile.GetLump<TexDataLump>();

        var texData1 = new TexData { TexName = "test/example" };
        var texData2 = new TexData { TexName = "test/example" };
        var texData3 = new TexData { TexName = "test/different" };
        texDataLump.Data.Add(texData1);
        texDataLump.Data.Add(texData2);
        texDataLump.Data.Add(texData3);

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt",
            [UpdateType.TexData]
        );

        // Assert
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
        // Arrange
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

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "materials/test/example.vmt",
            "materials/test/renamed.vmt"
        );

        // Assert
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
        // Arrange
        BspFile bspFile = TestUtils.CreateMockBspFile();
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();
        GameLump gameLump = bspFile.GetLump<GameLump>();
        Sprp sprpLump = gameLump.GetLump<Sprp>()!;
        sprpLump.StaticPropsDict!.Data.Add("models/test/example.mdl");

        // TODO next: doing something completely wrong here, check sprp in debug with real bsp

        // Act
        List<UpdateType> changes = pakfileLump.UpdatePathReferences(
            "models/test/example.mdl",
            "models/test/renamed.mdl",
            [UpdateType.StaticProp]
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.EqualTo(new List<UpdateType> { UpdateType.StaticProp }));
            Assert.That(sprpLump.StaticPropsDict.Data[0], Is.EqualTo("models/test/renamed.mdl"));
        });
    }
}
