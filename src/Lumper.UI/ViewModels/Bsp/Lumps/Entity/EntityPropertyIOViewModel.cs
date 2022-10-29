using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Lumper.Lib.BSP.Struct;
using Lumper.UI.Models;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;

public class EntityPropertyIOViewModel : EntityPropertyBase
{
    private float _delay;
    private string _input;
    private string _parameter;
    private string _targetEntityName;
    private int _timeToFire;
    private readonly EntityIO _value;

    public EntityPropertyIOViewModel(EntityViewModel parent, Lib.BSP.Struct.Entity.Property<EntityIO> property) : base(
        parent, property)
    {
        _value = property.Value;
        _targetEntityName = _value.TargetEntityName;
        _input = _value.Input;
        _parameter = _value.Parameter;
        _delay = _value.Delay;
        _timeToFire = _value.TimesToFire;
    }

    public string TargetEntityName
    {
        get => _targetEntityName;
        set
        {
            this.RaiseAndSetIfChanged(ref _targetEntityName, value);
            _value.TargetEntityName = value;
        }
    }

    public string Input
    {
        get => _input;
        set
        {
            this.RaiseAndSetIfChanged(ref _input, value);
            _value.Input = value;
        }
    }

    public string Parameter
    {
        get => _parameter;
        set
        {
            this.RaiseAndSetIfChanged(ref _parameter, value);
            _value.Parameter = value;
        }
    }

    public float Delay
    {
        get => _delay;
        set
        {
            this.RaiseAndSetIfChanged(ref _delay, value);
            _value.Delay = value;
        }
    }

    public int TimesToFire
    {
        get => _timeToFire;
        set
        {
            this.RaiseAndSetIfChanged(ref _timeToFire, value);
            _value.TimesToFire = value;
        }
    }

    protected override async ValueTask<bool> Match(Matcher matcher, CancellationToken? cancellationToken)
    {
        var match = false;
        match |= await matcher.Match(_targetEntityName);
        match |= await matcher.Match(_input);
        match |= await matcher.Match(_parameter);
        match |= await matcher.Match(_delay.ToString(CultureInfo.InvariantCulture));
        match |= await matcher.Match(_timeToFire.ToString());
        return match || await base.Match(matcher, cancellationToken);
        ;
    }
}