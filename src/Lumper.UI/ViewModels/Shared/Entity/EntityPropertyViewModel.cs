namespace Lumper.UI.ViewModels.Shared.Entity;

using Lib.BSP.Struct;
using Models.Matchers;

public abstract class EntityPropertyViewModel(Entity.EntityProperty entityProperty) : MatchableBspNode
{
    public Entity.EntityProperty EntityProperty { get; } = entityProperty;

    private string _key = entityProperty.Key;
    public string Key
    {
        get => _key;
        set
        {
            var wasClassname = _key == "classname";
            if (!UpdateField(ref _key, value) || this is not EntityPropertyStringViewModel vm)
                return;

            if (wasClassname)
                ((EntityViewModel)Parent).ResetClassName();

            else if (value == "classname")
                ((EntityViewModel)Parent).Name = vm.Value!;
        }
    }

    public void Delete() => ((EntityViewModel)Parent).DeleteProperty(this);
}

public class EntityPropertyStringViewModel(Entity.EntityProperty<string> entityProperty)
    : EntityPropertyViewModel(entityProperty)
{
    private string? _value = entityProperty.Value;
    public string? Value
    {
        get => _value;
        set
        {
            UpdateField(ref _value, value);

            if (Key == "classname")
                ((EntityViewModel)Parent).Name = value!; // Fine if this is null
        }
    }

    public override void UpdateModel() => entityProperty.Value = Value;

    public override bool Match(Matcher matcher) => matcher.Match(Value);
}

public class EntityPropertyIoViewModel(Entity.EntityProperty<EntityIo> entityProperty)
    : EntityPropertyViewModel(entityProperty)
{
    private string? _targetEntityName = entityProperty.Value?.TargetEntityName;
    public string? TargetEntityName
    {
        get => _targetEntityName;
        set => UpdateField(ref _targetEntityName, value);
    }

    private string? _input = entityProperty.Value?.Input;
    public string? Input
    {
        get => _input;
        set => UpdateField(ref _input, value);
    }

    private string? _parameter = entityProperty.Value?.Parameter;
    public string? Parameter
    {
        get => _parameter;
        set => UpdateField(ref _parameter, value);
    }

    private float? _delay = entityProperty.Value?.Delay;
    public float? Delay
    {
        get => _delay;
        set => UpdateField(ref _delay, value);
    }

    private int? _timeToFire = entityProperty.Value?.TimesToFire;
    public int? TimesToFire
    {
        get => _timeToFire;
        set => UpdateField(ref _timeToFire, value);
    }

    public override void UpdateModel()
    {
        if (entityProperty.Value is null)
            return;

        entityProperty.Value.TargetEntityName = TargetEntityName;
        entityProperty.Value.Input = Input;
        entityProperty.Value.Delay = Delay;
        entityProperty.Value.Parameter = Parameter;
        entityProperty.Value.TimesToFire = TimesToFire;
    }

    // Not bothering with IEquatable cus it's extra faff
    public bool Equals(EntityPropertyIoViewModel other)
        => other.TargetEntityName == TargetEntityName &&
           other.Input == Input &&
           other.Parameter == Parameter &&
           other.Delay == Delay &&
           other.TimesToFire == TimesToFire;

    public override bool Match(Matcher matcher) =>
        matcher.Match(TargetEntityName)
        || matcher.Match(Input)
        || matcher.Match(Parameter)
        || matcher.Match(Delay.ToString())
        || matcher.Match(TimesToFire.ToString());
}
