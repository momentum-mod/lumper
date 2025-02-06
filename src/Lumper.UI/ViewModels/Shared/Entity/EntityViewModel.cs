namespace Lumper.UI.ViewModels.Shared.Entity;

using System;
using System.Linq;
using DynamicData.Binding;
using Lumper.Lib.Bsp.Struct;
using ReactiveUI;

public class EntityViewModel : HierarchicalBspNode
{
    public Entity Entity { get; }

    public const string MissingClassname = "<missing classname!>";

    public ObservableCollectionExtended<EntityPropertyViewModel> Properties { get; } = [];

    private string? _classname;
    public string Classname
    {
        get => !string.IsNullOrWhiteSpace(_classname) ? _classname : "<missing classname!>";
        set => this.RaiseAndSetIfChanged(ref _classname, value);
    }

    public EntityViewModel(Entity entity, EntityLumpViewModel parent)
        : base(parent)
    {
        Entity = entity;

        foreach (Entity.EntityProperty property in entity.Properties)
            AddPropertyViewModel(property);

        ResetClassname();
    }

    public EntityPropertyViewModel AddPropertyViewModel(Entity.EntityProperty entityProperty)
    {
        EntityPropertyViewModel newProp = entityProperty switch
        {
            Entity.EntityProperty<string> sp => new EntityPropertyStringViewModel(sp, this),
            Entity.EntityProperty<EntityIo> sio => new EntityPropertyIoViewModel(sio, this),
            _ => throw new ArgumentOutOfRangeException(nameof(entityProperty)),
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

    public void ResetClassname() =>
        Classname = Properties
            .OfType<EntityPropertyStringViewModel>()
            .FirstOrDefault(x => x.Key == "classname")
            ?.Value!;

    public string PresentableName
    {
        get
        {
            string? hammerid = Properties
                .OfType<EntityPropertyStringViewModel>()
                .FirstOrDefault(x => x.Key == "hammerid")
                ?.Value;
            return $"{Classname} (HammerID {hammerid})";
        }
    }
}
