namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;

/// <summary>
///     Base ViewModel for <see cref="Lib.BSP.Struct.Entity.Property" />.
/// </summary>
public abstract class EntityPropertyBase : BspNodeBase
{
    public EntityPropertyBase(EntityViewModel parent,
        Lib.BSP.Struct.Entity.Property property)
        : base(parent)
    {
        Key = property.Key;
    }

    public string Key
    {
        get;
    }

    public override string NodeName => Key;
}
