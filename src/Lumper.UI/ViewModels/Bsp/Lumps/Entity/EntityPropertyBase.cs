namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;
/// <summary>
///     Base ViewModel for <see cref="Lib.BSP.Struct.Entity.Property" />.
/// </summary>
public abstract class EntityPropertyBase(
    EntityViewModel parent,
    Lib.BSP.Struct.Entity.Property property) : BspNodeBase(parent)
{
    private string _key = property.Key;
    public string Key
    {
        get => _key;
        set => Modify(ref _key, value);
    }

    public override string NodeName => Key;

    public Lib.BSP.Struct.Entity.Property Property { get; } = property;

    public override void Update()
    {
        Property.Key = _key;
        base.Update();
    }
    public void Delete()
    {
        if (Parent is EntityViewModel vm)
            vm.Delete(this);
    }
}
