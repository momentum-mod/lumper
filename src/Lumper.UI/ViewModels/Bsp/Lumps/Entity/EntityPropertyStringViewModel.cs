using System.Threading;
using System.Threading.Tasks;
using Lumper.UI.Models;

namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;

/// <summary>
///     ViewModel for <see cref="string" /> <see cref="Lib.BSP.Struct.Entity.Property" />.
/// </summary>
public class EntityPropertyStringViewModel : EntityPropertyBase
{
    private readonly Lib.BSP.Struct.Entity.Property<string> _property;
    private string _value;

    public EntityPropertyStringViewModel(EntityViewModel parent,
        Lib.BSP.Struct.Entity.Property<string> property)
        :
        base(parent, property)
    {
        _property = property;
        _value = property.Value;
    }

    public override BspNodeBase? ViewNode => Parent;

    public override bool IsModified =>
        base.IsModified || _property.Value != _value;

    public string Value
    {
        get => _value;
        set => Modify(ref _value, value);
    }

    public override void Update()
    {
        _property.Value = _value;
        base.Update();
    }

    protected override async ValueTask<bool> Match(Matcher matcher,
        CancellationToken? cancellationToken)
    {
        return await matcher.Match(_value)
               || await base.Match(matcher, cancellationToken);
    }
}
