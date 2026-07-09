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

public abstract class EntityPropertyViewModel : HierarchicalBspNode
{
    public Entity.EntityProperty Property { get; }
    public EntityViewModel ParentEntity { get; }

    // Note that EntityPropertyViewModels all store copies of their corresponding model values;
    // without we'd be unable to tell if a viewmodel has been modified when a job or other
    // Lumper.Lib code is ran that updates the model. When this happens, calling PullChangesFromModel
    // can simply try setting the viewmodel value to model value, and only RaisePropertyChanged
    // if it changed.
    private string _key;
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

    protected EntityPropertyViewModel(Entity.EntityProperty property, BspNode bspNode)
        : base(bspNode)
    {
        Property = property;
        ParentEntity = (EntityViewModel)bspNode;
        _key = property.Key;

        this.WhenAnyValue(x => x.ParentEntity.Classname)
            .Subscribe(classname => KeySuggestions = FetchKeySuggestionsForClassname(classname));
    }

    [Reactive]
    public IReadOnlyCollection<ExtendedAutoCompleteItem> KeySuggestions { get; set; } = [];

    public IReadOnlyCollection<ExtendedAutoCompleteItem> TargetEntityNameSuggestions =>
        BspService.Instance.TargetnameIndex.Suggestions;

    public List<ExtendedAutoCompleteItem> FetchKeySuggestionsForClassname(string classname)
    {
        List<ExtendedAutoCompleteItem> suggestions = [];
        if (
            !string.IsNullOrWhiteSpace(classname)
            && MomentumFGD.Entities.TryGetValue(classname, out FGDEntity? fgdEntity)
        )
        {
            foreach (FGDProperty prop in fgdEntity.Properties.Values)
            {
                var item = new ExtendedAutoCompleteItem { Value = prop.Name };

                suggestions.Add(item);
            }
        }
        return suggestions;
    }

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

public class EntityPropertyStringViewModel : EntityPropertyViewModel
{
    private readonly Entity.EntityProperty<string> _property;
    private string _value;
    private IReadOnlyCollection<ExtendedAutoCompleteItem>? _valueSuggestions;

    public EntityPropertyStringViewModel(Entity.EntityProperty<string> property, BspNode bspNode)
        : base(property, bspNode)
    {
        _property = property;
        _value = property.Value;

        this.WhenAnyValue(x => x.Key)
            .Subscribe(_ =>
            {
                IsBitfield = IsKeyBitfield();
                _valueSuggestions = null;
                this.RaisePropertyChanged(nameof(ValueSuggestions));
            });

        this.WhenAnyValue(x => x.ParentEntity.Classname)
            .Subscribe(_ =>
            {
                if (Key != "classname")
                {
                    IsBitfield = IsKeyBitfield();
                    _valueSuggestions = null;
                    this.RaisePropertyChanged(nameof(ValueSuggestions));
                }
            });
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
            return fgdProp.ValueType == FGDValueType.Flags;
        }
        return false;
    }

    private IReadOnlyCollection<ExtendedAutoCompleteItem> FetchValueSuggestionsForKey()
    {
        List<ExtendedAutoCompleteItem> suggestions = [];

        if (Key == "classname")
        {
            foreach (string? fgdKey in MomentumFGD.Entities.Keys)
            {
                var item = new ExtendedAutoCompleteItem { Value = fgdKey };
                suggestions.Add(item);
            }
        }
        else if (
            MomentumFGD.Entities.TryGetValue(ParentEntity.Classname, out FGDEntity? fgdEntity)
            && fgdEntity.Properties.TryGetValue(Key, out FGDProperty? fgdProp)
        )
        {
            if (fgdProp.ValueType == FGDValueType.Boolean)
            {
                suggestions.Add(new ExtendedAutoCompleteItem { Value = 0, Display = "[0] False" });
                suggestions.Add(new ExtendedAutoCompleteItem { Value = 1, Display = "[1] True" });
            }
            else if(fgdProp.ValueType == FGDValueType.TargetDestination)
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
                        Display = fgdProp.ValueType == FGDValueType.Flags ? choice.Value : $"[{choice.Key}] {choice.Value}",
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

        this.WhenAnyValue(x => x.ParentEntity.Classname).Subscribe(_ => KeySuggestions = FetchIoKeySuggestions());

        this.WhenAnyValue(x => x.TargetEntityName)
            .Subscribe(_ =>
            {
                _inputSuggestions = null;
                this.RaisePropertyChanged(nameof(InputSuggestions));
            });
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

    private List<ExtendedAutoCompleteItem> FetchIoKeySuggestions()
    {
        List<ExtendedAutoCompleteItem> suggestions = [];

        if (MomentumFGD.Entities.TryGetValue(ParentEntity.Classname, out FGDEntity? fgdEntity))
        {
            foreach (KeyValuePair<string, FGDOutput> output in fgdEntity.Outputs)
            {
                var item = new ExtendedAutoCompleteItem { Value = output.Value.Name };
                suggestions.Add(item);
            }
        }
        return suggestions;
    }

    private List<ExtendedAutoCompleteItem> FetchInputSuggestions()
    {
        List<ExtendedAutoCompleteItem> suggestions = [];

        string? classname = BspService
            .Instance.TargetnameIndex.Entries.FirstOrDefault(e => e.Targetname == TargetEntityName)
            ?.Classname;

        if (classname != null && MomentumFGD.Entities.TryGetValue(classname, out FGDEntity? fgdEntity))
        {
            foreach (FGDInput input in fgdEntity.Inputs.Values)
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
