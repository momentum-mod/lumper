using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;

/// <summary>
///     ViewModel for <see cref="Lib.BSP.Struct.Entity" />.
/// </summary>
public class EntityViewModel : BspNodeBase
{
    private readonly string _className;
    private readonly Lib.BSP.Struct.Entity _entity;

    public EntityViewModel(EntityLumpViewModel parent,
        Lib.BSP.Struct.Entity entity)
        : base(parent)
    {
        _className = entity.ClassName;
        _entity = entity;
        foreach (var property in entity.Properties)
            AddProperty(property);
        InitializeNodeChildrenObserver(Properties);

        this.WhenAnyValue(x => x.IsModified)
           .ObserveOn(RxApp.MainThreadScheduler)
           .Where(m => m == true)
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

    private bool _isModified = false;
    public override bool IsModified =>
        _isModified
        || (Nodes is { Count: > 0 } && Nodes.Any(n => n.IsModified));

    private void AddProperty(Lib.BSP.Struct.Entity.Property property)
    {
        EntityPropertyBase propertyViewModel = property switch
        {
            Lib.BSP.Struct.Entity.Property<string> sp =>
                new EntityPropertyStringViewModel(this, sp),
            Lib.BSP.Struct.Entity.Property<EntityIO> sio =>
                new EntityPropertyIOViewModel(this, sio),
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };
        Properties.Add(propertyViewModel);
    }

    public void AddString()
    {
        var prop = new Lib.BSP.Struct.Entity.Property<string>(
                "newproperty", "newvalue");
        Add(prop);
    }

    public void AddIO()
    {
        var prop = new Lib.BSP.Struct.Entity.Property<EntityIO>(
                    "newproperty", new EntityIO());
        Add(prop);
    }

    private void Add(Lib.BSP.Struct.Entity.Property prop)
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
