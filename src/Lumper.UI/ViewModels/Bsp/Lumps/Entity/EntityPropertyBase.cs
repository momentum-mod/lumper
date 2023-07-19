using System.Reactive.Linq;
using ReactiveUI;

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
        _key = property.Key;
        _property = property;
    }

    private string _key;
    public string Key
    {
        get => _key;
        set => Modify(ref _key, value);
    }

    public override string NodeName => Key;

    private readonly Lib.BSP.Struct.Entity.Property _property;
    public Lib.BSP.Struct.Entity.Property Property => _property;

    public override void Update()
    {
        _property.Key = _key;
        base.Update();
    }
    public void Delete()
    {
        if (Parent is EntityViewModel vm)
            vm.Delete(this);
    }
}
