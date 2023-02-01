using System;
using System.Linq;
using DynamicData;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;

/// <summary>
///     ViewModel for <see cref="Lib.BSP.Struct.Entity" />.
/// </summary>
public class EntityViewModel : BspNodeBase
{
    private readonly string _className;

    public EntityViewModel(EntityLumpViewModel parent,
        Lib.BSP.Struct.Entity entity)
        : base(parent)
    {
        _className = entity.ClassName;
        foreach (var property in entity.Properties)
            AddProperty(property);
        InitializeNodeChildrenObserver(Properties);
    }

    public SourceList<EntityPropertyBase> Properties
    {
        get;
    } = new();

    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"Entity{(string.IsNullOrWhiteSpace(_className) ? "" : $" ({_className})")}";

    public override bool IsModified =>
        Nodes is { Count: > 0 } && Nodes.Any(n => n.IsModified);

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
}
