namespace Lumper.UI.ViewModels.Shared.Entity;

using System;
using System.Linq;
using DynamicData.Binding;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.ExtensionMethods;
using Lumper.UI.Services;
using ReactiveUI;

public class EntityViewModel : HierarchicalBspNode
{
    public Entity Entity { get; }

    public ObservableCollectionExtended<EntityPropertyViewModel> Properties { get; } = [];

    public const string MissingClassname = "<missing classname!>";

    private string? _classname;
    public string Classname
    {
        get => !string.IsNullOrWhiteSpace(_classname) ? _classname : MissingClassname;
        set => this.RaiseAndSetIfChanged(ref _classname, value);
    }

    public EntityViewModel(Entity entity, EntityLumpViewModel parent)
        : base(parent)
    {
        Entity = entity;

        foreach (Entity.EntityProperty property in entity.Properties)
            Properties.Add(CreatePropertyViewModel(property));

        ResetClassname();
    }

    public EntityPropertyViewModel AddProperty(Entity.EntityProperty prop)
    {
        EntityPropertyViewModel vm = CreatePropertyViewModel(prop);
        Entity.Properties.Add(prop);
        Properties.Add(vm);
        MarkAsModified();
        return vm;
    }

    public void DeleteProperty(EntityPropertyViewModel propVm)
    {
        Entity.Properties.Remove(propVm.EntityProperty);
        Properties.Remove(propVm);
        MarkAsModified();
    }

    private EntityPropertyViewModel CreatePropertyViewModel(Entity.EntityProperty entityProperty) =>
        entityProperty switch
        {
            Entity.EntityProperty<string> sp => new EntityPropertyStringViewModel(sp, this),
            Entity.EntityProperty<EntityIo> sio => new EntityPropertyIoViewModel(sio, this),
            _ => throw new ArgumentOutOfRangeException(nameof(entityProperty)),
        };

    public void AddString() => AddProperty(new Entity.EntityProperty<string>("newproperty", "newvalue"));

    public void AddIo()
    {
        EntityLump? el = BspService.Instance.BspFile?.GetLump<EntityLump>();

        if (el is null)
            return;

        // Try to figure out which separator to use. Comma will work in any game, whilst ESC
        // will be totally broken in some, so use comma unless totally sure ESC is okay: either
        // entity lump version is 1 (Strata only), or if we have any existing entities using ESC.
        char separator =
            el.Version == 1
            || el.Data.Any(ent =>
                ent.Properties.Any(prop => prop is Entity.EntityProperty<EntityIo> { Value.Separator: '\u001b' })
            )
                ? '\u001b'
                : ',';

        AddProperty(new Entity.EntityProperty<EntityIo>("newproperty", new EntityIo(separator)));
    }

    public override void UpdateModel()
    {
        foreach (EntityPropertyViewModel prop in Properties)
            prop.UpdateModel();
    }

    public string? FindProperty(string key) =>
        Properties.OfType<EntityPropertyStringViewModel>().FirstOrDefault(x => x.Key == key)?.Value;

    public void ResetClassname() => Classname = FindProperty("classname")!;

    public string? Origin => FindProperty("origin");

    public string? HammerId => FindProperty("hammerid");

    public string? Targetname => FindProperty("targetname");

    public string PresentableName => $"{Classname} (HammerID {HammerId})";

    public string ClassAndTargetname => Targetname is { } tn ? $"{Classname} ({tn})" : Classname;

    public bool MatchClassname(string expr, bool wildcardWrapping) =>
        _classname?.MatchesSimpleExpression(expr, wildcardWrapping) ?? false;

    public void TeleportToMe()
    {
        if (Origin is { } origin)
            GameSyncService.Instance.TeleportToOrigin(origin);
    }
}
