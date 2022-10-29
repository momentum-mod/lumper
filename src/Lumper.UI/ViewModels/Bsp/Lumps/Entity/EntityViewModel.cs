using System;
using DynamicData;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;

public class EntityViewModel : BspNodeBase
{
    private readonly string _className;

    public EntityViewModel(EntityLumpViewModel parent, Lib.BSP.Struct.Entity entity) : base(parent)
    {
        _className = entity.ClassName;
        foreach (var property in entity.Properties)
            AddProperty(property);
        InitializeNodeChildrenObserver(Properties);
    }

    public SourceList<EntityPropertyBase> Properties { get; } = new();

    public override bool CanView => true;
    public override string NodeName => $"Entity{(string.IsNullOrWhiteSpace(_className) ? "" : $" ({_className})")}";

    private void AddProperty(Lib.BSP.Struct.Entity.Property property)
    {
        EntityPropertyBase propertyViewModel = property switch
        {
            Lib.BSP.Struct.Entity.Property<string> sp => new EntityPropertyStringViewModel(this, sp),
            Lib.BSP.Struct.Entity.Property<EntityIO> sio => new EntityPropertyIOViewModel(this, sio),
            _ => throw new ArgumentOutOfRangeException(nameof(property))
        };
        Properties.Add(propertyViewModel);
    }
}