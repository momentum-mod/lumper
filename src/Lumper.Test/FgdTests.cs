namespace Lumper.Test;

using System.Linq;
using Lumper.Lib.Fgd;

[TestFixture]
public class FgdTests
{
    [Test]
    public void Parse_ParsesPropertiesChoicesAndIo()
    {
        const string input = """
            // Base properties should be inherited
            @BaseClass = BaseEntity [
                spawnflags(flags) : "Flags" : 0 : "Spawn flags" = [ 2 : "Second" 1 : "First" ]
                input Enable(void) : "Enables the entity"
            ]

            @PointClass base(BaseEntity) = logic_test : "Test entity" [
                target(target_destination) : "Target"
                enabled(boolean) : "Enabled" : 1 : "Whether enabled"
                linedivider(choices) : "Visual divider"
                output OnDone(string) : "Raised when complete"
            ]
            """;

        IReadOnlyDictionary<string, FgdEntity> entities = Fgd.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(entities, Has.Count.EqualTo(1));
            Assert.That(entities.ContainsKey("logic_test"), Is.True);
        });

        FgdEntity entity = entities["logic_test"];
        Assert.Multiple(() =>
        {
            Assert.That(entity.ClassType, Is.EqualTo("PointClass"));
            Assert.That(entity.Description, Is.EqualTo("Test entity"));
            Assert.That(entity.Properties.ContainsKey("linedivider"), Is.False);
            Assert.That(entity.Properties.ContainsKey("spawnflags"), Is.True);
            Assert.That(entity.Properties["target"].ValueType, Is.EqualTo(FgdValueType.TargetDestination));
            Assert.That(entity.Properties["enabled"].ValueType, Is.EqualTo(FgdValueType.Boolean));
            Assert.That(entity.Outputs["OnDone"].ParameterType, Is.EqualTo("string"));
            Assert.That(entity.Inputs.ContainsKey("Enable"), Is.True);
        });

        string[] choiceKeys = entities["logic_test"].Properties["spawnflags"].Choices.Keys.ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(choiceKeys, Has.Length.EqualTo(2));
            Assert.That(choiceKeys[0], Is.EqualTo("1"));
            Assert.That(choiceKeys[1], Is.EqualTo("2"));
        });
    }

    [Test]
    public void ResolveInheritance_MergesBaseMembersAndSkipsBaseClasses()
    {
        const string input = """
            @BaseClass = BaseEntity [
                inherited(string) : "Inherited"
                input Enable(void) : "Enable"
            ]

            @PointClass base(BaseEntity) = logic_test : "Test entity" [
                own(string) : "Own"
                output OnDone(void) : "Done"
            ]
            """;

        IReadOnlyDictionary<string, FgdEntity> entities = Fgd.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(entities, Has.Count.EqualTo(1));
            Assert.That(entities.ContainsKey("logic_test"), Is.True);
        });

        FgdEntity entity = entities["logic_test"];
        Assert.Multiple(() =>
        {
            Assert.That(entity.Properties.ContainsKey("inherited"), Is.True);
            Assert.That(entity.Properties.ContainsKey("own"), Is.True);
            Assert.That(entity.Inputs.ContainsKey("Enable"), Is.True);
            Assert.That(entity.Outputs.ContainsKey("OnDone"), Is.True);
        });
    }
}
