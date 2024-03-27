namespace Lumper.UI.ViewModels.EntityEditor;
using System.Threading;
using System.Threading.Tasks;
using Lib.BSP.Struct;
using Lumper.UI.Models;

/// <summary>
///     ViewModel for <see cref="string" /> <see cref="Entity.EntityProperty" />.
/// </summary>
public class EntityPropertyStringViewModel(Entity.EntityProperty<string> entityProperty)
    : EntityPropertyBase(entityProperty)
{
    private string Property { get; } = entityProperty.Value;

    private string _value = entityProperty.Value;
    private string Value
    {
        get => _value;
        set => UpdateField(ref _value, value);
    }


    protected override async ValueTask<bool> Match(Matcher matcher, CancellationToken? _) =>
        await matcher.Match(Value);
}
