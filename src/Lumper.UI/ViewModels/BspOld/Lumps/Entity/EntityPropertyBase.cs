namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;
using Lib.BSP.Struct;

/// <summary>
///     Base ViewModel for <see cref="Entity.EntityProperty" />.
/// </summary>
public abstract class EntityPropertyBase(
    EntityViewModel parent,
    Entity.EntityProperty entityProperty) : BspNodeBase(parent)
{
    private string _key = entityProperty.Key;
    public string Key
    {
        get => _key;
        set => Modify(ref _key, value);
    }

    public override string NodeName => Key;

    public Entity.EntityProperty EntityProperty { get; } = entityProperty;

    public override void Update()
    {
        EntityProperty.Key = _key;
        base.Update();
    }
    public void Delete()
    {
        if (Parent is EntityViewModel vm)
            vm.Delete(this);
    }
}
