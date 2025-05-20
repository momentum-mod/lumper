namespace Lumper.Test;

using System.IO;
using System.Linq;
using System.Text;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.Stripper;
using NUnit.Framework;

[TestFixture]
public class StripperConfigTests
{
    private BspFile _bspFile;
    private EntityLump _entityLump;

    [SetUp]
    public void Setup()
    {
        _bspFile = TestUtils.CreateMockBspFile();
        _entityLump = _bspFile.GetLump<EntityLump>();

        // Clear any entities that might be in the lump
        _entityLump.Data.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        _bspFile.Dispose();
    }

    [Test]
    public void TestAddBlock()
    {
        // Create a StripperConfig with an Add block
        const string configText = """
            add:
            {
                "classname" "func_button"
                "targetname" "test_button"
                "origin" "0 0 0"
            }
            """;
        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that the entity was added
        Assert.That(_entityLump.Data, Has.Count.EqualTo(1));
        Entity entity = _entityLump.Data.First();

        Assert.Multiple(() =>
        {
            Assert.That(entity.Properties, Has.Count.EqualTo(3));
            Assert.That(GetPropertyValue(entity, "classname"), Is.EqualTo("func_button"));
            Assert.That(GetPropertyValue(entity, "targetname"), Is.EqualTo("test_button"));
            Assert.That(GetPropertyValue(entity, "origin"), Is.EqualTo("0 0 0"));
        });
    }

    [Test]
    public void TestFilterBlock()
    {
        // Add test entities
        AddTestEntity("func_button", "button1", "0 0 0");
        AddTestEntity("func_door", "door1", "100 100 0");
        AddTestEntity("func_button", "button2", "200 0 0");

        // Create a StripperConfig with a Filter block to remove all func_button entities
        const string configText = """
            filter:
            {
                "classname" "func_button"
            }
            """;
        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that only the func_door remains
        Assert.That(_entityLump.Data, Has.Count.EqualTo(1));
        Entity entity = _entityLump.Data.First();
        Assert.Multiple(() =>
        {
            Assert.That(GetPropertyValue(entity, "classname"), Is.EqualTo("func_door"));
            Assert.That(GetPropertyValue(entity, "targetname"), Is.EqualTo("door1"));
        });
    }

    [Test]
    public void TestModifyBlockReplace()
    {
        // Add test entities
        AddTestEntity("func_button", "button1", "0 0 0");
        AddTestEntity("func_door", "door1", "100 100 0");

        // Create a StripperConfig with a Modify block to change properties
        const string configText = """
            modify:
            {
                match:
                {
                    "classname" "func_button"
                }
                replace:
                {
                    "targetname" "modified_button"
                    "origin" "50 50 50"
                }
            }
            """;
        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that the func_button was modified
        Entity buttonEntity = _entityLump.Data.First(e => GetPropertyValue(e, "classname") == "func_button");
        Assert.Multiple(() =>
        {
            Assert.That(GetPropertyValue(buttonEntity, "targetname"), Is.EqualTo("modified_button"));
            Assert.That(GetPropertyValue(buttonEntity, "origin"), Is.EqualTo("50 50 50"));
        });

        // Check that the func_door was not modified
        Entity doorEntity = _entityLump.Data.First(e => GetPropertyValue(e, "classname") == "func_door");
        Assert.That(GetPropertyValue(doorEntity, "targetname"), Is.EqualTo("door1"));
    }

    [Test]
    public void TestModifyBlockDelete()
    {
        // Add test entity with extra property
        var entity = new Entity();
        entity.Properties.Add(Entity.EntityProperty.Create("classname", "func_button")!);
        entity.Properties.Add(Entity.EntityProperty.Create("targetname", "button1")!);
        entity.Properties.Add(Entity.EntityProperty.Create("origin", "0 0 0")!);
        entity.Properties.Add(Entity.EntityProperty.Create("extra_prop", "value")!);
        _entityLump.Data.Add(entity);

        // Create a StripperConfig to delete a property
        const string configText = """
            modify:
            {
                match:
                {
                    "classname" "func_button"
                }
                delete:
                {
                    "extra_prop" "value"
                }
            }
            """;
        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that the property was deleted
        entity = _entityLump.Data.First();
        Assert.That(entity.Properties, Has.Count.EqualTo(3));
        Assert.That(entity.Properties.Any(p => p.Key == "extra_prop"), Is.False);
    }

    [Test]
    public void TestModifyBlockInsert()
    {
        // Add test entity
        AddTestEntity("func_button", "button1", "0 0 0");

        // Create a StripperConfig to insert a new property
        const string configText = """
            modify:
            {
                match:
                {
                    "classname" "func_button"
                }
                insert:
                {
                    "new_prop" "new_value"
                }
            }
            """;
        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that the property was inserted
        Entity entity = _entityLump.Data.First();
        Assert.Multiple(() =>
        {
            Assert.That(entity.Properties, Has.Count.EqualTo(4));
            Assert.That(GetPropertyValue(entity, "new_prop"), Is.EqualTo("new_value"));
        });
    }

    [Test]
    public void TestRegexMatching()
    {
        // Add test entities with different values
        AddTestEntity("func_button", "button_123", "0 0 0");
        AddTestEntity("func_button", "button_abc", "100 0 0");
        AddTestEntity("func_button", "other_name", "200 0 0");

        // Create a StripperConfig with a regex filter
        string configText = """
            filter:
            {
                "targetname" "/button_.*/"
            }
            """;
        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that only the non-matching entity remains
        Assert.That(_entityLump.Data, Has.Count.EqualTo(1));
        Entity entity = _entityLump.Data.First();
        Assert.That(GetPropertyValue(entity, "targetname"), Is.EqualTo("other_name"));
    }

    [Test]
    public void TestParseInvalidConfig()
    {
        const string configText = """
            invalid_block:
            {
                "key" "value"
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(configText));
        Assert.Throws<NotImplementedException>(() => StripperConfig.Parse(stream));

        stream.Seek(0, SeekOrigin.Begin);

        bool success = StripperConfig.TryParse(stream, out StripperConfig? config, out string? errorMessage);
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(config, Is.Null);
            Assert.That(errorMessage, Is.Not.Null);
        });
    }

    [Test]
    public void TestMultipleBlocksInOneConfig()
    {
        // Create a StripperConfig with multiple blocks
        const string configText = """
            add:
            {
                "classname" "func_button"
                "targetname" "added_button"
                "origin" "0 0 0"
            }
            add:
            {
                "classname" "func_door"
                "targetname" "added_door"
                "origin" "100 100 100"
            }
            filter:
            {
                "targetname" "added_button"
            }
            """;

        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that only the door was added (button was added then filtered)
        Assert.That(_entityLump.Data, Has.Count.EqualTo(1));
        Entity entity = _entityLump.Data.First();

        Assert.Multiple(() =>
        {
            Assert.That(GetPropertyValue(entity, "classname"), Is.EqualTo("func_door"));
            Assert.That(GetPropertyValue(entity, "targetname"), Is.EqualTo("added_door"));
        });
    }

    [Test]
    public void TestCaseInsensitiveMatching()
    {
        // Add test entities
        AddTestEntity("FUNC_BUTTON", "Button1", "0 0 0");

        // Create a StripperConfig using lowercase for matching
        const string configText = """
            modify:
            {
                match:
                {
                    "classname" "func_button"
                }
                replace:
                {
                    "targetname" "modified_button"
                }
            }
            """;

        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that the entity was modified despite case differences
        Entity entity = _entityLump.Data.First();
        Assert.That(GetPropertyValue(entity, "targetname"), Is.EqualTo("modified_button"));
    }

    [Test]
    public void TestFilterWithMultipleCriteria()
    {
        // Add test entities
        AddTestEntity("func_button", "button1", "0 0 0");
        AddTestEntity("func_button", "button2", "100 0 0");
        AddTestEntity("func_door", "door1", "200 0 0");

        // Create a StripperConfig that filters on multiple properties
        const string configText = """
            filter:
            {
                "classname" "func_button"
                "targetname" "button1"
            }
            """;

        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that only the matching entity was removed
        Assert.That(_entityLump.Data, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(_entityLump.Data.Any(e => GetPropertyValue(e, "targetname") == "button1"), Is.False);
            Assert.That(_entityLump.Data.Any(e => GetPropertyValue(e, "targetname") == "button2"), Is.True);
            Assert.That(_entityLump.Data.Any(e => GetPropertyValue(e, "targetname") == "door1"), Is.True);
        });
    }

    [Test]
    public void TestModifyMultipleEntities()
    {
        // Add test entities
        AddTestEntity("func_button", "button1", "0 0 0");
        AddTestEntity("func_button", "button2", "100 0 0");
        AddTestEntity("func_door", "door1", "200 0 0");

        // Create a StripperConfig to modify all buttons
        const string configText = """
            modify:
            {
                match:
                {
                    "classname" "func_button"
                }
                insert:
                {
                    "wait" "5"
                }
            }
            """;

        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check that all buttons were modified
        var buttons = _entityLump.Data.Where(e => GetPropertyValue(e, "classname") == "func_button").ToList();
        Assert.That(buttons, Has.Count.EqualTo(2));

        foreach (Entity button in buttons)
            Assert.That(GetPropertyValue(button, "wait"), Is.EqualTo("5"));

        // Check that door was not modified
        Entity door = _entityLump.Data.First(e => GetPropertyValue(e, "classname") == "func_door");
        Assert.That(door.Properties.Any(p => p.Key == "wait"), Is.False);
    }

    [Test]
    public void TestComplexScenario()
    {
        // Add test entities
        AddTestEntity("func_button", "button1", "0 0 0");
        AddTestEntity("func_button", "button2", "100 0 0");
        AddTestEntity("func_door", "door1", "200 0 0");

        // Create a complex StripperConfig with multiple operations
        const string configText = """
            // First add a new entity
            add:
            {
                "classname" "logic_auto"
                "targetname" "auto1"
                "origin" "0 0 0"
            }

            // Then modify existing buttons
            modify:
            {
                match:
                {
                    "classname" "func_button"
                }
                insert:
                {
                    "speed" "10"
                }
            }

            // Remove one specific button
            filter:
            {
                "targetname" "button1"
            }

            // Add another entity
            add:
            {
                "classname" "info_player_start"
                "origin" "50 50 50"
                "angles" "0 0 0"
            }
            """;

        StripperConfig config = ParseConfig(configText);

        // Apply the config
        ApplyConfig(config, _entityLump);

        // Check final state:
        // - button1 should be removed
        // - button2 should have speed=10
        // - door1 should be unchanged
        // - auto1 should be added
        // - info_player_start should be added

        Assert.That(_entityLump.Data, Has.Count.EqualTo(4));

        // Check button2
        Entity? button2 = _entityLump.Data.FirstOrDefault(e =>
            GetPropertyValue(e, "classname") == "func_button" && GetPropertyValue(e, "targetname") == "button2"
        );

        Assert.Multiple(() =>
        {
            Assert.That(button2, Is.Not.Null);
            Assert.That(GetPropertyValue(button2!, "speed"), Is.EqualTo("10"));

            // Check added entities
            Assert.That(_entityLump.Data.Count(e => GetPropertyValue(e, "classname") == "logic_auto"), Is.EqualTo(1));
            Assert.That(
                _entityLump.Data.Count(e => GetPropertyValue(e, "classname") == "info_player_start"),
                Is.EqualTo(1)
            );
        });

        // Check unchanged door
        Entity? door = _entityLump.Data.FirstOrDefault(e => GetPropertyValue(e, "classname") == "func_door");
        Assert.That(door, Is.Not.Null);
    }

    // Helper methods
    private static StripperConfig ParseConfig(string configText)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(configText));
        return StripperConfig.Parse(stream);
    }

    private static void ApplyConfig(StripperConfig config, EntityLump entityLump)
    {
        foreach (StripperConfig.Block block in config.Blocks)
            block.Apply(entityLump);
    }

    private void AddTestEntity(string className, string targetName, string origin)
    {
        var entity = new Entity();
        entity.Properties.Add(Entity.EntityProperty.Create("classname", className)!);
        entity.Properties.Add(Entity.EntityProperty.Create("targetname", targetName)!);
        entity.Properties.Add(Entity.EntityProperty.Create("origin", origin)!);
        _entityLump.Data.Add(entity);
    }

    private static string? GetPropertyValue(Entity entity, string key)
    {
        return entity.Properties.FirstOrDefault(p => p.Key == key)?.ValueString;
    }
}
