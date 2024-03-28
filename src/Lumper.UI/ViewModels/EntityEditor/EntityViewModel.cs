namespace Lumper.UI.ViewModels.EntityEditor;
using System;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Lumper.Lib.BSP.Struct;
using Models;

/// <summary>
///     ViewModel representing an <see cref="_entity" />.
/// </summary>
public class EntityViewModel : BspNodeBase
{
    private readonly EntityEditorViewModel _parent;
    private readonly Entity _entity;
    private readonly SourceList<EntityPropertyBaseViewModel> _properties = new();

    public EntityViewModel(EntityEditorViewModel parent, Entity entity)
    {
        _entity = entity;
        _parent = parent;
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
        _properties.Add(entityProperty switch
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
        _entity.Properties.Add(prop);
        MarkAsModified();
    }

    public void Delete(EntityPropertyBaseViewModel prop)
    {
        _properties.Remove(prop);
        _entity.Properties.Remove(prop.EntityProperty);
        MarkAsModified();
    }

    public void CloseTab() => _parent.CloseTab(this);

    protected override async ValueTask<bool> Match(Matcher matcher, CancellationToken? _) =>
        await matcher.Match(Name);

    // TODO: So confused by Update()...
    // public override void Update()
    // {
    //     _isModified = false;
    //     base.Update();
    // }
}
