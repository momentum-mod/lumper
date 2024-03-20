namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Lumper.Lib.BSP.Struct;
using Lumper.UI.Models;

/// <summary>
///     ViewModel for <see cref="EntityIO" /> <see cref="Entity.Property" />.
/// </summary>
public class EntityPropertyIOViewModel : EntityPropertyBase
{
    private readonly EntityIO _entity;
    private float _delay;
    private string _input;
    private string _parameter;
    private string _targetEntityName;
    private int _timeToFire;

    public EntityPropertyIOViewModel(EntityViewModel parent,
        Entity.Property<EntityIO> property)
        : base(parent, property)
    {
        _entity = property.Value;
        _targetEntityName = _entity.TargetEntityName;
        _input = _entity.Input;
        _parameter = _entity.Parameter;
        _delay = _entity.Delay;
        _timeToFire = _entity.TimesToFire;
    }

    public override BspNodeBase? ViewNode => Parent;

    public override bool IsModified => base.IsModified
                                       || _entity.TargetEntityName
                                       != _targetEntityName
                                       || _entity.Input != _input
                                       || _entity.Parameter != _parameter
                                       // ReSharper disable once CompareOfFloatsByEqualityOperator
                                       || _entity.Delay != _delay
                                       || _entity.TimesToFire != _timeToFire;

    public string TargetEntityName
    {
        get => _targetEntityName;
        set => Modify(ref _targetEntityName, value);
    }

    public string Input
    {
        get => _input;
        set => Modify(ref _input, value);
    }

    public string Parameter
    {
        get => _parameter;
        set => Modify(ref _parameter, value);
    }

    public float Delay
    {
        get => _delay;
        set => Modify(ref _delay, value);
    }

    public int TimesToFire
    {
        get => _timeToFire;
        set => Modify(ref _timeToFire, value);
    }

    public override void Update()
    {
        _entity.TargetEntityName = _targetEntityName;
        _entity.Input = _input;
        _entity.Parameter = _parameter;
        _entity.Delay = _delay;
        _entity.TimesToFire = _timeToFire;
        base.Update();
    }

    protected override async ValueTask<bool> Match(Matcher matcher,
        CancellationToken? cancellationToken) => await matcher.Match(_targetEntityName)
                                                 || await matcher.Match(_input)
                                                 || await matcher.Match(_parameter)
                                                 || await matcher.Match(
                                                     _delay.ToString(CultureInfo.InvariantCulture))
                                                 || await matcher.Match(_timeToFire.ToString())
                                                 || await base.Match(matcher, cancellationToken);
}
