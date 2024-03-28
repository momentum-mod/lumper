namespace Lumper.UI.ViewModels.EntityEditor;
using System;
using System.Collections.Generic;
using DynamicData;
using Lib.BSP.Struct;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Services;

/// <summary>
///     ViewModel representing an <see cref="_entityLump" />.
/// </summary>
public sealed partial class EntityEditorViewModel : ViewModelBase, IDisposable
{
    private SourceList<EntityViewModel> _entities = new();
    private EntityLump _entityLump;

    public EntityEditorViewModel()
    {
        if (!ActiveBspService.Instance.HasLoadedBsp)
            throw new InvalidOperationException();

        _entityLump = ActiveBspService.Instance.BspFile!.GetLump<EntityLump>();

        foreach (Entity entity in _entityLump.Data)
            _entities.Add(new EntityViewModel(this, entity));
    }

    public void AddEntity()
    {
        var entity = new Entity(new List<KeyValuePair<string, string>>());
        _entityLump.Data.Add(entity);
        _entities.Add(new EntityViewModel(this, entity));
    }

    public void Dispose() => _entities.Dispose();
}
