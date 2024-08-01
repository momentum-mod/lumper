namespace Lumper.UI.ViewModels.Shared.Entity;

using System;
using System.Linq;
using DynamicData.Binding;
using Lib.BSP.Struct;
using Models.Matchers;
using ReactiveUI;

public class EntityViewModel : MatchableBspNode
{
    public Entity Entity { get; }
    public ObservableCollectionExtended<EntityPropertyViewModel> Properties { get; } = [];

    private string? _name;
    public string Name
    {
        get => !string.IsNullOrWhiteSpace(_name) ? _name : "<missing classname!>";
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public EntityViewModel(Entity entity, BspNode parent) : base(parent)
    {
        Entity = entity;

        foreach (Entity.EntityProperty property in entity.Properties)
            AddPropertyViewModel(property);

        ResetClassName();
    }

    public EntityPropertyViewModel AddPropertyViewModel(Entity.EntityProperty entityProperty)
    {
        EntityPropertyViewModel newProp = entityProperty switch {
            Entity.EntityProperty<string> sp => new EntityPropertyStringViewModel(sp, this),
            Entity.EntityProperty<EntityIo> sio => new EntityPropertyIoViewModel(sio, this),
            _ => throw new ArgumentOutOfRangeException(nameof(entityProperty))
        };
        Properties.Add(newProp);
        return newProp;
    }

    private void AddProperty(Entity.EntityProperty prop)
    {
        Entity.Properties.Add(prop);
        AddPropertyViewModel(prop);
        MarkAsModified();
    }

    public void AddString() => AddProperty(new Entity.EntityProperty<string>("newproperty", "newvalue"));

    public void AddIo() => AddProperty(new Entity.EntityProperty<EntityIo>("newproperty", new EntityIo()));

    public void DeleteProperty(EntityPropertyViewModel propVm)
    {
        Entity.Properties.Remove(propVm.EntityProperty);
        Properties.Remove(propVm);
        MarkAsModified();
    }

    public override void UpdateModel()
    {
        foreach (EntityPropertyViewModel prop in Properties)
            prop.UpdateModel();
    }

    public void ResetClassName()
        => Name = Properties
            .OfType<EntityPropertyStringViewModel>()
            .FirstOrDefault(x => x.Key == "classname")?
            .Value!;

    public string PresentableName
    {
        get
        {
            var hammerid = Properties
                .OfType<EntityPropertyStringViewModel>()
                .FirstOrDefault(x => x.Key == "hammerid")?
                .Value;
            return $"{Name} (HammerID {hammerid})";
        }
    }

    public override bool Match(Matcher matcher) => Properties.Any(item => item.Match(matcher));
}
