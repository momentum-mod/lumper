namespace Lumper.UI.ViewModels.EntityEditor;
using Lib.BSP.Struct;

/// <summary>
///     Base ViewModel for <see cref="Entity.EntityProperty" />.
/// </summary>
public abstract class EntityPropertyBaseViewModel(Entity.EntityProperty entityProperty) : BspNodeBase
{
    public Entity.EntityProperty EntityProperty { get; } = entityProperty;

    private string _key = entityProperty.Key;
    public string Key
    {
        get => _key;
        set => UpdateField(ref _key, value);
    }
}
