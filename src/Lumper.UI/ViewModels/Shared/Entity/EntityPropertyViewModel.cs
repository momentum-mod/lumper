namespace Lumper.UI.ViewModels.Shared.Entity;

using System.Globalization;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.ExtensionMethods;

public abstract class EntityPropertyViewModel(Entity.EntityProperty entityProperty, BspNode bspNode)
    : HierarchicalBspNode(bspNode)
{
    public Entity.EntityProperty EntityProperty { get; } = entityProperty;

    private string _key = entityProperty.Key;

    public string Key
    {
        get => _key;
        set
        {
            bool wasClassname = _key == "classname";
            if (!UpdateField(ref _key, value) || this is not EntityPropertyStringViewModel vm)
                return;

            if (wasClassname)
                ((EntityViewModel)Parent).ResetClassname();
            else if (value == "classname")
                ((EntityViewModel)Parent).Classname = vm.Value!;
        }
    }

    public override void UpdateModel() => EntityProperty.Key = Key;

    public bool MatchKey(string expr, bool wildcardWrapping = false) =>
        Key.MatchesSimpleExpression(expr, wildcardWrapping);

    public abstract bool MatchValue(string expr, bool trailingWildcard = false);

    public void Delete() => ((EntityViewModel)Parent).DeleteProperty(this);
}

public class EntityPropertyStringViewModel(Entity.EntityProperty<string> entityProperty, BspNode bspNode)
    : EntityPropertyViewModel(entityProperty, bspNode)
{
    private string? _value = entityProperty.Value;

    public string? Value
    {
        get => _value;
        set
        {
            UpdateField(ref _value, value);

            if (Key == "classname")
                ((EntityViewModel)Parent).Classname = value!; // Fine if this is null
        }
    }

    public override void UpdateModel()
    {
        base.UpdateModel();
        entityProperty.Value = Value;
    }

    public override bool MatchValue(string expr, bool trailingWildcard) =>
        Value?.MatchesSimpleExpression(expr, trailingWildcard) ?? false;
}

public class EntityPropertyIoViewModel(Entity.EntityProperty<EntityIo> entityProperty, BspNode bspNode)
    : EntityPropertyViewModel(entityProperty, bspNode)
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
        base.UpdateModel();

        if (entityProperty.Value is null)
            return;

        entityProperty.Value.TargetEntityName = TargetEntityName;
        entityProperty.Value.Input = Input;
        entityProperty.Value.Delay = Delay;
        entityProperty.Value.Parameter = Parameter;
        entityProperty.Value.TimesToFire = TimesToFire;
    }

    // Not bothering with IEquatable cus it's extra faff
    public bool Equals(EntityPropertyIoViewModel other) =>
        other.TargetEntityName == TargetEntityName
        && other.Input == Input
        && other.Parameter == Parameter
        && other.Delay == Delay
        && other.TimesToFire == TimesToFire;

    // csharpier-ignore
    public override bool MatchValue(string expr, bool trailingWildcard) =>
        (TargetEntityName?.MatchesSimpleExpression(expr, trailingWildcard) ?? false) ||
        (Input?.MatchesSimpleExpression(expr, trailingWildcard) ?? false) ||
        (Parameter?.MatchesSimpleExpression(expr, trailingWildcard) ?? false) ||
        (Delay?.ToString(CultureInfo.InvariantCulture).MatchesSimpleExpression(expr, trailingWildcard) ?? false) ||
        (TimesToFire?.ToString(CultureInfo.InvariantCulture).MatchesSimpleExpression(expr, trailingWildcard) ?? false);
}
