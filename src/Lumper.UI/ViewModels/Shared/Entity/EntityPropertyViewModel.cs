namespace Lumper.UI.ViewModels.Shared.Entity;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.ExtensionMethods;
using Lumper.Lib.FGD;
using Lumper.UI.Controls;
using Lumper.UI.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public abstract class EntityPropertyViewModel(Entity.EntityProperty property, BspNode bspNode)
    : HierarchicalBspNode(bspNode)
{
    public Entity.EntityProperty Property { get; } = property;
    public EntityViewModel ParentEntity { get; } = (EntityViewModel)bspNode;

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
            bool wasTargetname = _key == "targetname";

            if (_key == value)
                return;

            _key = value;
            Property.Key = value;
            this.RaisePropertyChanged();
            OnKeyChanged();

            if (this is not EntityPropertyStringViewModel vm)
                return;

            if (wasClassname)
                ParentEntity.ResetClassname();
            else if (value == "classname")
                ParentEntity.Classname = vm.Value;

            if (wasTargetname || value == "targetname")
                ParentEntity.RaiseTargetnameChanged();
        }
    }

    protected virtual void OnKeyChanged() { }

    [Reactive]
    public IReadOnlyCollection<ExtendedAutoCompleteItem> KeySuggestions { get; set; } = [];

    public IReadOnlyCollection<ExtendedAutoCompleteItem> TargetEntityNameSuggestions =>
        BspService.Instance.TargetnameIndex.Suggestions;

    private static readonly Dictionary<string, IReadOnlyCollection<ExtendedAutoCompleteItem>> KeySuggestionsCache = [];

    public virtual IReadOnlyCollection<ExtendedAutoCompleteItem> FetchKeySuggestionsForClassname(string classname)
    {
        if (string.IsNullOrWhiteSpace(classname))
            return [];

        if (KeySuggestionsCache.TryGetValue(classname, out IReadOnlyCollection<ExtendedAutoCompleteItem>? cached))
            return cached;

        List<ExtendedAutoCompleteItem> suggestions = [];
        if (MomentumFGD.Entities.TryGetValue(classname, out FGDEntity? fgdEntity))
        {
            foreach (FgdProperty prop in fgdEntity.Properties.Values)
            {
                suggestions.Add(new ExtendedAutoCompleteItem { Value = prop.Name });
            }
        }

        KeySuggestionsCache[classname] = suggestions;
        return suggestions;
    }

    public void RefreshForClassname(string classname)
    {
        KeySuggestions = FetchKeySuggestionsForClassname(classname);
        OnClassnameChanged();
    }

    protected virtual void OnClassnameChanged() { }

    public static EntityPropertyViewModel Create(Entity.EntityProperty entityProperty, EntityViewModel parent)
    {
        return entityProperty switch
        {
            Entity.EntityProperty<string> sp => new EntityPropertyStringViewModel(sp, parent),
            Entity.EntityProperty<EntityIo> sio => new EntityPropertyIoViewModel(sio, parent),
            _ => throw new ArgumentOutOfRangeException(nameof(entityProperty)),
        };
    }

    public abstract bool MemberwiseEquals(EntityPropertyViewModel other);

    public bool MatchKey(string expr, bool wildcardWrapping)
    {
        return Key.MatchesSimpleExpression(expr, wildcardWrapping);
    }

    public abstract bool MatchValue(string expr, bool wildcardWrapping);

    public void Delete()
    {
        ParentEntity.DeleteProperty(this);
    }
}

public class EntityPropertyStringViewModel(Entity.EntityProperty<string> property, BspNode bspNode)
    : EntityPropertyViewModel(property, bspNode)
{
    private readonly Entity.EntityProperty<string> _property = property;
    private string _value = property.Value;
    private IReadOnlyCollection<ExtendedAutoCompleteItem>? _valueSuggestions;
    private static readonly IReadOnlyCollection<ExtendedAutoCompleteItem> ClassnameSuggestions = MomentumFGD
        .Entities.Keys.Select(fgdKey => new ExtendedAutoCompleteItem { Value = fgdKey })
        .ToList();

    protected override void OnKeyChanged()
    {
        IsBitfield = IsKeyBitfield();
        _valueSuggestions = null;
        this.RaisePropertyChanged(nameof(ValueSuggestions));
    }

    protected override void OnClassnameChanged()
    {
        if (Key == "classname")
            return;

        IsBitfield = IsKeyBitfield();
        _valueSuggestions = null;
        this.RaisePropertyChanged(nameof(ValueSuggestions));
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;

            _value = value;
            _property.Value = value;
            MarkAsModified();
            this.RaisePropertyChanged();

            if (Key == "classname")
                ParentEntity.Classname = value;
            else if (Key == "targetname")
                ParentEntity.RaiseTargetnameChanged();
        }
    }

    public IReadOnlyCollection<ExtendedAutoCompleteItem> ValueSuggestions =>
        _valueSuggestions ??= FetchValueSuggestionsForKey();

    [Reactive]
    public bool IsBitfield { get; set; } = false;

    private bool IsKeyBitfield()
    {
        if (
            MomentumFGD.Entities.TryGetValue(ParentEntity.Classname, out FGDEntity? fgdEntity)
            && fgdEntity.Properties.TryGetValue(Key, out FGDProperty? fgdProp)
        )
        {
            return fgdProp.ValueType == FgdValueType.Flags;
        }
        return false;
    }

    private IReadOnlyCollection<ExtendedAutoCompleteItem> FetchValueSuggestionsForKey()
    {
        List<ExtendedAutoCompleteItem> suggestions = [];

        if (Key == "classname")
        {
            return ClassnameSuggestions;
        }
        else if (
            MomentumFGD.Entities.TryGetValue(ParentEntity.Classname, out FGDEntity? fgdEntity)
            && fgdEntity.Properties.TryGetValue(Key, out FGDProperty? fgdProp)
        )
        {
            if (fgdProp.ValueType == FgdValueType.Boolean)
            {
                suggestions.Add(new ExtendedAutoCompleteItem { Value = 0, Display = "[0] False" });
                suggestions.Add(new ExtendedAutoCompleteItem { Value = 1, Display = "[1] True" });
            }
            else if (fgdProp.ValueType == FgdValueType.TargetDestination)
            {
                return BspService.Instance.TargetnameIndex.Suggestions;
            }
            else
            {
                IReadOnlyDictionary<string, string> choices = fgdProp.Choices;

                foreach (KeyValuePair<string, string> choice in choices)
                {
                    var item = new ExtendedAutoCompleteItem
                    {
                        Value = choice.Key,
                        Display =
                            fgdProp.ValueType == FgdValueType.Flags ? choice.Value : $"[{choice.Key}] {choice.Value}",
                    };
                    suggestions.Add(item);
                }
            }
        }
        return suggestions;
    }

    public override bool MemberwiseEquals(EntityPropertyViewModel other)
    {
        return other is EntityPropertyStringViewModel o && o.Key == Key && o.Value == Value;
    }

    public override bool MatchValue(string expr, bool wildcardWrapping)
    {
        return Value.MatchesSimpleExpression(expr, wildcardWrapping);
    }
}

// ReSharper disable CompareOfFloatsByEqualityOperator
public class EntityPropertyIoViewModel : EntityPropertyViewModel
{
    private readonly Entity.EntityProperty<EntityIo> _property;
    private string _targetEntityName;
    private string _input;
    private IReadOnlyCollection<ExtendedAutoCompleteItem>? _inputSuggestions;
    private string _parameter;
    private float _delay;
    private int _timesToFire;

    public EntityPropertyIoViewModel(Entity.EntityProperty<EntityIo> property, BspNode bspNode)
        : base(property, bspNode)
    {
        _property = property;
        Key = property.Key;
        _targetEntityName = property.Value.TargetEntityName;
        _input = property.Value.Input;
        _parameter = property.Value.Parameter;
        _delay = property.Value.Delay;
        _timesToFire = property.Value.TimesToFire;
    }

    public string TargetEntityName
    {
        get => _targetEntityName;
        set
        {
            if (_targetEntityName == value)
                return;

            _targetEntityName = value;
            _property.Value.TargetEntityName = value;
            this.RaisePropertyChanged();

            _inputSuggestions = null;
            this.RaisePropertyChanged(nameof(InputSuggestions));

            OnValueChanged();
        }
    }

    public string Input
    {
        get => _input;
        set
        {
            if (_input == value)
                return;

            _input = value;
            _property.Value.Input = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    public IReadOnlyCollection<ExtendedAutoCompleteItem> InputSuggestions =>
        _inputSuggestions ??= FetchInputSuggestions();

    public string Parameter
    {
        get => _parameter;
        set
        {
            if (_parameter == value)
                return;

            _parameter = value;
            _property.Value.Parameter = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    public float Delay
    {
        get => _delay;
        set
        {
            if (_delay == value)
                return;

            _delay = value;
            _property.Value.Delay = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    public int TimesToFire
    {
        get => _timesToFire;
        set
        {
            if (_timesToFire == value)
                return;

            _timesToFire = value;
            _property.Value.TimesToFire = value;
            this.RaisePropertyChanged();
            OnValueChanged();
        }
    }

    public string DisplayValue => _property.Value.ToString();

    private void OnValueChanged()
    {
        MarkAsModified();
        this.RaisePropertyChanged(nameof(DisplayValue));
    }

    private static readonly Dictionary<string, IReadOnlyCollection<ExtendedAutoCompleteItem>> IoKeySuggestionsCache =
    [];

    public override IReadOnlyCollection<ExtendedAutoCompleteItem> FetchKeySuggestionsForClassname(string classname)
    {
        if (string.IsNullOrWhiteSpace(classname))
            return [];

        if (IoKeySuggestionsCache.TryGetValue(classname, out IReadOnlyCollection<ExtendedAutoCompleteItem>? cached))
            return cached;

        List<ExtendedAutoCompleteItem> suggestions = [];
        if (MomentumFGD.Entities.TryGetValue(classname, out FGDEntity? fgdEntity))
        {
            foreach (KeyValuePair<string, FgdOutput> output in fgdEntity.Outputs)
            {
                suggestions.Add(new ExtendedAutoCompleteItem { Value = output.Value.Name });
            }
        }

        IoKeySuggestionsCache[classname] = suggestions;
        return suggestions;
    }

    private List<ExtendedAutoCompleteItem> FetchInputSuggestions()
    {
        List<ExtendedAutoCompleteItem> suggestions = [];

        if (
            BspService.Instance.TargetnameIndex.ClassnamesByTargetname.TryGetValue(
                TargetEntityName,
                out string? classname
            )
            && classname != null
            && MomentumFGD.Entities.TryGetValue(classname, out FGDEntity? fgdEntity)
        )
        {
            foreach (FgdInput input in fgdEntity.Inputs.Values)
            {
                var item = new ExtendedAutoCompleteItem { Value = input.Name };
                suggestions.Add(item);
            }
        }

        return suggestions;
    }

    public override bool MemberwiseEquals(EntityPropertyViewModel other)
    {
        return other is EntityPropertyIoViewModel o
            && o.Key == Key
            && o.TargetEntityName == TargetEntityName
            && o.Input == Input
            && o.Parameter == Parameter
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            && o.Delay == Delay
            && o.TimesToFire == TimesToFire;
    }

    public override bool MatchValue(string expr, bool wildcardWrapping)
    {
        // Match against both comma and space separated values
        return string.Create(
                    CultureInfo.InvariantCulture,
                    $"{TargetEntityName} {Input} {Parameter} {Delay} {TimesToFire}"
                )
                .MatchesSimpleExpression(expr, wildcardWrapping)
            || string.Create(
                    CultureInfo.InvariantCulture,
                    $"{TargetEntityName},{Input},{Parameter},{Delay},{TimesToFire}"
                )
                .MatchesSimpleExpression(expr, wildcardWrapping);
    }
}
