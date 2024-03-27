namespace Lumper.UI.ViewModels.EntityEditor;
using System;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Lumper.Lib.BSP.Struct;
using Models;

/// <summary>
///     ViewModel representing an <see cref="Entity" />.
/// </summary>
public class EntityViewModel : BspNodeBase
{
    private Entity Entity { get; }
    private SourceList<EntityPropertyBase> Properties { get; } = new();

    public EntityViewModel(Entity entity)
    {
        Entity = entity;
        _className = entity.ClassName;

        foreach (Entity.EntityProperty property in entity.Properties)
            AddProperty(property);

        // TODO: Huh?
        // this.WhenAnyValue(x => x.IsModified)
        //     .ObserveOn(RxApp.MainThreadScheduler)
        //     .Where(m => m)
        //     .Subscribe(_ =>
        //     {
        //         if (parent is EntityLumpViewModel entityLump)
        //             entityLump.Open();
        //     });
    }

    private string? _className;
    private string? ClassName
    {
        get => _className;
        set => UpdateField(ref _className, value);
    }


    public string Name => !string.IsNullOrWhiteSpace(_className) ? ClassName! : "<missing classname>";

    private void AddProperty(Entity.EntityProperty entityProperty) =>
        Properties.Add(entityProperty switch
        {
            Entity.EntityProperty<string> sp => new EntityPropertyStringViewModel(sp),
            Entity.EntityProperty<EntityIO> sio => new EntityPropertyIOViewModel(sio),
            _ => throw new ArgumentOutOfRangeException(nameof(entityProperty))
        });

    public void AddString() => Add(new Entity.EntityProperty<string>("newproperty", "newvalue"));

    public void AddIO() => Add(new Entity.EntityProperty<EntityIO>("newproperty", new EntityIO()));

    private void Add(Entity.EntityProperty prop)
    {
        AddProperty(prop);
        Entity.Properties.Add(prop);
        MarkAsModified();
    }

    public void Delete(EntityPropertyBase prop)
    {
        Properties.Remove(prop);
        Entity.Properties.Remove(prop.EntityProperty);
        MarkAsModified();
    }

    protected override async ValueTask<bool> Match(Matcher matcher, CancellationToken? _) =>
        await matcher.Match(Name);

    // TODO: So confused by Update()...
    // public override void Update()
    // {
    //     _isModified = false;
    //     base.Update();
    // }
}
