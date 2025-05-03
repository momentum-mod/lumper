using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using UpdateType = Lumper.Lib.Bsp.Lumps.BspLumps.PakfileLump.PathReferenceUpdateType;

namespace Lumper.Test;

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
}
