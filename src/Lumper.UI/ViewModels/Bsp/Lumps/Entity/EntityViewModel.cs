namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;
using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Lumper.Lib.BSP.Struct;
using ReactiveUI;

/// <summary>
///     ViewModel for <see cref="Entity" />.
/// </summary>
public class EntityViewModel : BspNodeBase
{
    private readonly string _className;
    private readonly Entity _entity;

    public EntityViewModel(EntityLumpViewModel parent,
        Entity entity)
        : base(parent)
    {
        _className = entity.ClassName;
        _entity = entity;
        foreach (Entity.Property property in entity.Properties)
            AddProperty(property);
        InitializeNodeChildrenObserver(Properties);

        this.WhenAnyValue(x => x.IsModified)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(m => m)
            .Subscribe(_ =>
            {
                if (parent is EntityLumpViewModel entityLump)
                    entityLump.Open();
            });
    }

    public SourceList<EntityPropertyBase> Properties
    {
        get;
    } = new();

    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"Entity{(string.IsNullOrWhiteSpace(_className) ? "" : $" ({_className})")}";

    private bool _isModified;
    public override bool IsModified =>
        _isModified
        || (Nodes is { Count: > 0 } && Nodes.Any(n => n.IsModified));

    private void AddProperty(Entity.Property property)
    {
        EntityPropertyBase propertyViewModel = property switch
        {
            Entity.Property<string> sp =>
                new EntityPropertyStringViewModel(this, sp),
            Entity.Property<EntityIO> sio =>
                new EntityPropertyIOViewModel(this, sio),
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };
        Properties.Add(propertyViewModel);
    }

    public void AddString()
    {
        var prop = new Entity.Property<string>(
            "newproperty", "newvalue");
        Add(prop);
    }

    public void AddIO()
    {
        var prop = new Entity.Property<EntityIO>(
            "newproperty", new EntityIO());
        Add(prop);
    }

    private void Add(Entity.Property prop)
    {
        AddProperty(prop);
        _entity.Properties.Add(prop);
        _isModified = true;
        this.RaisePropertyChanged(nameof(IsModified));
    }

    public void Delete(EntityPropertyBase prop)
    {
        Properties.Remove(prop);
        _entity.Properties.Remove(prop.Property);
        _isModified = true;
        this.RaisePropertyChanged(nameof(IsModified));
    }

    public override void Update()
    {
        _isModified = false;
        base.Update();
    }
}
