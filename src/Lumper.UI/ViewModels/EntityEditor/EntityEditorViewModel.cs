namespace Lumper.UI.ViewModels.EntityEditor;
using System;
using System.Collections.Generic;
using DynamicData;
using Lib.BSP.Struct;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Services;

/// <summary>
///     ViewModel representing an <see cref="EntityLump" />.
/// </summary>
public sealed class EntityEditorViewModel : ViewModelBase, IDisposable
{
    private SourceList<EntityViewModel> Entities { get; } = new();
    private EntityLump EntityLump { get; }

    public EntityEditorViewModel()
    {
        if (!ActiveBspService.Instance.HasLoadedBsp)
            throw new InvalidOperationException();

        EntityLump = ActiveBspService.Instance.BspFile!.GetLump<EntityLump>();

        foreach (Entity entity in EntityLump.Data)
            Entities.Add(new EntityViewModel(entity));
    }

    public void AddEntity()
    {
        var entity = new Entity(new List<KeyValuePair<string, string>>());
        EntityLump.Data.Add(entity);
        Entities.Add(new EntityViewModel(entity));
    }

    public void Dispose() => Entities.Dispose();
}
