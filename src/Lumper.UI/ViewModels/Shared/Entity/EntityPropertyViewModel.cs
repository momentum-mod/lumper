namespace Lumper.UI.ViewModels.Shared.Entity;

using System.Globalization;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.ExtensionMethods;
using ReactiveUI;

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

    public bool MatchKey(string expr, bool wildcardWrapping) => Key.MatchesSimpleExpression(expr, wildcardWrapping);

    public abstract bool MatchValue(string expr, bool wildcardWrapping);

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

    public override bool MatchValue(string expr, bool wildcardWrapping) =>
        Value?.MatchesSimpleExpression(expr, wildcardWrapping) ?? false;
}

public class EntityPropertyIoViewModel(Entity.EntityProperty<EntityIo> entityProperty, BspNode bspNode)
    : EntityPropertyViewModel(entityProperty, bspNode)
{
    private string? _targetEntityName = entityProperty.Value?.TargetEntityName;
    public string? TargetEntityName
    {
        get => _targetEntityName;
        set => UpdateFieldInternal(ref _targetEntityName, value);
    }

    private string? _input = entityProperty.Value?.Input;
    public string? Input
    {
        get => _input;
        set => UpdateFieldInternal(ref _input, value);
    }

    private string? _parameter = entityProperty.Value?.Parameter;
    public string? Parameter
    {
        get => _parameter;
        set => UpdateFieldInternal(ref _parameter, value);
    }

    private float? _delay = entityProperty.Value?.Delay;
    public float? Delay
    {
        get => _delay;
        set => UpdateFieldInternal(ref _delay, value);
    }

    private int? _timeToFire = entityProperty.Value?.TimesToFire;
    public int? TimesToFire
    {
        get => _timeToFire;
        set => UpdateFieldInternal(ref _timeToFire, value);
    }

    public string DisplayValue
    {
        get
        {
            // TODO: This is a gross copy of EntityIO's ToString() method.
            // These properties should *really* be get/setters around the model properties,
            // then this property could just be `=> entityProperty.ValueString`,
            // instead of duplicating the values here, but then handling IsModified after
            // job runs becomes a lot more complicated. (See BspNode.UpdateField summary)
            //
            // We don't have time for significant refactors at the moment so I'm going
            // to just leave it, but in the future we could come back and really try to
            // do the MVVM separation more appropriately.
            char separator = entityProperty.Value?.Separator ?? ',';
            // csharpier-ignore
            return _targetEntityName + separator
                + _input + separator
                + _parameter + separator
                + _delay?.ToString(CultureInfo.InvariantCulture) + separator
                + _timeToFire?.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void UpdateFieldInternal<T>(ref T field, T value)
    {
        UpdateField(ref field, value);
        this.RaisePropertyChanged(nameof(DisplayValue));
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

    public override bool MatchValue(string expr, bool wildcardWrapping) =>
        // Match against both comma and space separated values
        string.Create(CultureInfo.InvariantCulture, $"{TargetEntityName} {Input} {Parameter} {Delay} {TimesToFire}")
            .MatchesSimpleExpression(expr, wildcardWrapping)
        || string.Create(CultureInfo.InvariantCulture, $"{TargetEntityName},{Input},{Parameter},{Delay},{TimesToFire}")
            .MatchesSimpleExpression(expr, wildcardWrapping);
}
