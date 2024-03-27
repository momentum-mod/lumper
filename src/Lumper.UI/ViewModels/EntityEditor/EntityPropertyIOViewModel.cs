namespace Lumper.UI.ViewModels.EntityEditor;
using System.Threading;
using System.Threading.Tasks;
using Lumper.Lib.BSP.Struct;
using Lumper.UI.Models;

/// <summary>
///     ViewModel for <see cref="EntityIO" /> <see cref="Entity.EntityProperty" />.
/// </summary>
public class EntityPropertyIOViewModel(Entity.EntityProperty<EntityIO> entityProperty)
    : EntityPropertyBase(entityProperty)
{
    private EntityIO Property { get; } = entityProperty.Value;

    private string? _targetEntityName = entityProperty.Value.TargetEntityName;
    public string? TargetEntityName
    {
        get => _targetEntityName;
        set => UpdateField(ref _targetEntityName, value);
    }

    private string? _input = entityProperty.Value.Input;
    public string? Input
    {
        get => _input;
        set => UpdateField(ref _input, value);
    }

    private string? _parameter = entityProperty.Value.Parameter;
    public string? Parameter
    {
        get => _parameter;
        set => UpdateField(ref _parameter, value);
    }

    private float? _delay = entityProperty.Value.Delay;
    public float? Delay
    {
        get => _delay;
        set => UpdateField(ref _delay, value);
    }

    private int? _timeToFire = entityProperty.Value.TimesToFire;
    public int? TimesToFire
    {
        get => _timeToFire;
        set => UpdateField(ref _timeToFire, value);
    }

    protected override async ValueTask<bool> Match(Matcher matcher, CancellationToken? _) =>
           await matcher.Match(TargetEntityName)
        || await matcher.Match(Input)
        || await matcher.Match(Parameter)
        || await matcher.Match(Delay.ToString())
        || await matcher.Match(TimesToFire.ToString());
}
