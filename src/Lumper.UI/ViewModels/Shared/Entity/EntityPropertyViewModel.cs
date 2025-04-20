namespace Lumper.UI.ViewModels.Shared.Entity;

using System.Globalization;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.ExtensionMethods;
using ReactiveUI;

public abstract class EntityPropertyViewModel(Entity.EntityProperty property, BspNode bspNode)
    : HierarchicalBspNode(bspNode)
{
    public Entity.EntityProperty Property { get; } = property;

    // Note that EntityPropertyViewModels all store copies of their corresponding model values;
    // without we'd be unable to tell if a viewmodel has been modified when a job or other
    // Lumper.Lib code is ran that updates the model. When this happens, calling PullChangesFromModel
    // can simply try setting the viewmodel value to model value, and only RaisePropertyChanged
    // if it changed.
    private string _key = property.Key;
    public string Key
    {
        get => _key;
        set
        {
            bool wasClassname = _key == "classname";

            if (_key == value)
                return;

            _key = value;
            Property.Key = value;
            this.RaisePropertyChanged();

            if (this is not EntityPropertyStringViewModel vm)
                return;

            if (wasClassname)
                ((EntityViewModel)Parent).ResetClassname();
            else if (value == "classname")
                ((EntityViewModel)Parent).Classname = vm.Value;
        }
    }

    public abstract bool MemberwiseEquals(EntityPropertyViewModel other);

    public bool MatchKey(string expr, bool wildcardWrapping) => Key.MatchesSimpleExpression(expr, wildcardWrapping);

    public abstract bool MatchValue(string expr, bool wildcardWrapping);

    public void Delete() => ((EntityViewModel)Parent).DeleteProperty(this);
}

public class EntityPropertyStringViewModel(Entity.EntityProperty<string> property, BspNode bspNode)
    : EntityPropertyViewModel(property, bspNode)
{
    private string _value = property.Value;
    public string Value
    {
        get => property.Value;
        set
        {
            if (_value == value)
                return;

            _value = value;
            property.Value = value;
            MarkAsModified();
            this.RaisePropertyChanged();

            if (Key == "classname")
                ((EntityViewModel)Parent).Classname = value;
        }
    }

    public override bool MemberwiseEquals(EntityPropertyViewModel other) =>
        other is EntityPropertyStringViewModel o && o.Key == Key && o.Value == Value;

    public override bool MatchValue(string expr, bool wildcardWrapping) =>
        Value.MatchesSimpleExpression(expr, wildcardWrapping);
}

// ReSharper disable CompareOfFloatsByEqualityOperator
public class EntityPropertyIoViewModel(Entity.EntityProperty<EntityIo> property, BspNode bspNode)
    : EntityPropertyViewModel(property, bspNode)
{
    private string _targetEntityName = property.Value.TargetEntityName;
    public string TargetEntityName
    {
        get => property.Value.TargetEntityName;
        set
        {
            if (_targetEntityName == value)
                return;

            _targetEntityName = value;
            property.Value.TargetEntityName = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    private string _input = property.Value.Input;
    public string Input
    {
        get => _input;
        set
        {
            if (_input == value)
                return;

            _input = value;
            property.Value.Input = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    private string _parameter = property.Value.Parameter;
    public string Parameter
    {
        get => property.Value.Parameter;
        set
        {
            if (_parameter == value)
                return;

            _parameter = value;
            property.Value.Parameter = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    private float _delay = property.Value.Delay;
    public float Delay
    {
        get => _delay;
        set
        {
            if (_delay == value)
                return;

            _delay = value;
            property.Value.Delay = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    private int _timesToFire = property.Value.TimesToFire;
    public int TimesToFire
    {
        get => _timesToFire;
        set
        {
            if (_timesToFire == value)
                return;

            _timesToFire = value;
            property.Value.TimesToFire = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    public string DisplayValue => property.Value.ToString();

    private void OnValueChanged()
    {
        MarkAsModified();
        this.RaisePropertyChanged(nameof(DisplayValue));
    }

    public override bool MemberwiseEquals(EntityPropertyViewModel other) =>
        other is EntityPropertyIoViewModel o
        && o.Key == Key
        && o.TargetEntityName == TargetEntityName
        && o.Input == Input
        && o.Parameter == Parameter
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        && o.Delay == Delay
        && o.TimesToFire == TimesToFire;

    public override bool MatchValue(string expr, bool wildcardWrapping) =>
        // Match against both comma and space separated values
        string.Create(CultureInfo.InvariantCulture, $"{TargetEntityName} {Input} {Parameter} {Delay} {TimesToFire}")
            .MatchesSimpleExpression(expr, wildcardWrapping)
        || string.Create(CultureInfo.InvariantCulture, $"{TargetEntityName},{Input},{Parameter},{Delay},{TimesToFire}")
            .MatchesSimpleExpression(expr, wildcardWrapping);
}
