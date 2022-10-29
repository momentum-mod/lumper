using System.Threading;
using System.Threading.Tasks;
using Lumper.UI.Models;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;

public class EntityPropertyStringViewModel : EntityPropertyBase
{
    private readonly Lib.BSP.Struct.Entity.Property<string> _property;
    private string _value;

    public EntityPropertyStringViewModel(EntityViewModel parent, Lib.BSP.Struct.Entity.Property<string> property) :
        base(parent, property)
    {
        _property = property;
        _value = property.Value;
    }

    public string Value
    {
        get => _value;
        set
        {
            this.RaiseAndSetIfChanged(ref _value, value);
            _property.Value = value;
        }
    }


    protected override async ValueTask<bool> Match(Matcher matcher, CancellationToken? cancellationToken)
    {
        return await matcher.Match(_value)
               || await base.Match(matcher, cancellationToken);
    }
}