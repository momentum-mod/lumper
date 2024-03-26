namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;
using System.Threading;
using System.Threading.Tasks;
using Lib.BSP.Struct;
using Lumper.UI.Models;

/// <summary>
///     ViewModel for <see cref="string" /> <see cref="Entity.EntityProperty" />.
/// </summary>
public class EntityPropertyStringViewModel : EntityPropertyBase
{
    private readonly Entity.EntityProperty<string> _property;
    private string _value;

    public EntityPropertyStringViewModel(EntityViewModel parent, Entity.EntityProperty<string> entityProperty)
        : base(parent, entityProperty)
    {
        _property = entityProperty;
        _value = entityProperty.Value;
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
        CancellationToken? cancellationToken) => await matcher.Match(_value)
                                                 || await base.Match(matcher, cancellationToken);
}
