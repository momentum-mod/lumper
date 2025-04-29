namespace Lumper.UI.ViewModels.Shared.Entity;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Pages.RawEntities;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public sealed class EntityLumpViewModel : BspNode, ILumpViewModel
{
    private readonly EntityLump _entityLump;

    public SourceCache<EntityViewModel, Entity> Entities { get; } = new(ent => ent.Entity);

    [ObservableAsProperty]
    public int EntityCount { get; }

    public bool IsEditingStream { get; set; }

    // Don't want to have to include this but can't do saving without :(
    public RawEntitiesViewModel? RawEntitiesViewModel { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Required by BspService.LazyLoadLump
    public EntityLumpViewModel() => throw new NotImplementedException();

    // This class is created the first time something requests this lump from BspService,
    // and discarded when that BSP file is closed.
    public EntityLumpViewModel(BspFile bsp)
    {
        BspService.Instance.ThrowIfNoLoadedBsp();

        _entityLump = bsp.GetLump<EntityLump>();

        LoadEntityList();

        Entities.CountChanged.ObserveOn(RxApp.MainThreadScheduler).ToPropertyEx(this, x => x.EntityCount);
    }

    private void LoadEntityList() =>
        Entities.Edit(innerCache =>
        {
            innerCache.Clear();
            innerCache.AddOrUpdate(_entityLump.Data.Select(ent => new EntityViewModel(ent, this)));
        });

    // Note: don't use this for doing large inserts, you'll make the SourceCache fire a billion
    // update notifications. Use .Edit to batch all changes together:
    // (https://github.com/reactivemarbles/DynamicData?tab=readme-ov-file#the-observable-cache)
    public void AddEmptyEntity()
    {
        var entity = new Entity();
        // Adding this to the model now just because it's easier than adding any
        // new stuff from the viewmodel during UpdateModel - we'd have to track
        // new items separately, or do a slow search for existing entity models
        // for *every* entity, just to figure add what the new things are.
        _entityLump.Data.Add(entity);
        Entities.AddOrUpdate(new EntityViewModel(entity, this));
    }

    // Same as above!
    public void RemoveEntity(EntityViewModel entity)
    {
        _entityLump.Data.Remove(entity.Entity);
        Entities.Remove(entity);
        MarkAsModified();
    }

    public void RemoveMultiple(IEnumerable<EntityViewModel> entities) =>
        Entities.Edit(innerCache =>
        {
            foreach (EntityViewModel entity in entities)
            {
                _entityLump.Data.Remove(entity.Entity);
                innerCache.Remove(entity);
            }
        });

    /// <summary>
    /// Update the underlying model (EntityLump) with data from the ViewModel
    /// </summary>
    public override void UpdateModel()
    {
        if (IsEditingStream && RawEntitiesViewModel is not null)
        {
            RawEntitiesViewModel.SaveOrDiscardEntityLump();
        }
        else
        {
            foreach (EntityViewModel ent in Entities.Items)
                ent.UpdateModel();
        }
    }

    /// <summary>
    /// Get a MemoryStream of the lump, as ASCII text data
    /// </summary>
    public MemoryStream GetStream()
    {
        var stream = new MemoryStream();
        _entityLump.Write(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    /// <summary>
    /// Regenerates the entire entity lump for the text contents of a stream
    /// </summary>
    public void UpdateFromStream(MemoryStream stream)
    {
        // Pretty likely the user fucks something up here, so make a backup.
        // We have to implement all the clone interface crap just for this,
        // but don't see a good alternative.
        var originalData = _entityLump.Data.Select(x => (Entity)x.Clone()).ToHashSet();
        _entityLump.Data.Clear();

        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new BinaryReader(stream);
        try
        {
            _entityLump.Read(reader, stream.Length, strict: true);
        }
        catch (InvalidDataException ex)
        {
            _entityLump.Data = originalData;
            Logger.Error(ex, "Parser error! Lump has not been modified.");
        }

        stream.Dispose();

        LoadEntityList();
        this.RaisePropertyChanged();
    }

    /// <summary>
    /// Scan the underlying model for changes and update on the viewmodel.
    ///
    /// Use this for Jobs that affect this lump. Jobs are part of Lumper.Lib
    /// so have no effect on the viewmodel, we have figure them out programmatically.
    ///
    /// Fortunately since this class and EntityLump use a SourceCache and HashSet respectively,
    /// with a Entity-based key, we get constant-time lookup. This can handle a map with 10,000
    /// entities in ~25ms, whilst List-based version was ~3s.
    /// </summary>
    public void UpdateViewModelFromModel()
    {
        List<EntityViewModel> additions = [];
        List<Entity> removals = [];
        foreach (Entity ent in _entityLump.Data)
        {
            // In EL, not in ELVM -> add to ELVM
            Optional<EntityViewModel> entVm = Entities.Lookup(ent);
            if (!entVm.HasValue)
            {
                var newEntity = new EntityViewModel(ent, this);
                additions.Add(newEntity);
                continue;
            }

            // Property updates on EL -> update on ELVM
            foreach (EntityPropertyViewModel propVm in entVm.Value.Properties)
            {
                // Checking both references and underlying values. Jobs should generally
                // just create an entirely new property (so ref compare is what matters here)
                // so entityproperty ctor logic runs, but doesn't hurt to test for value
                // changes as well.
                switch (propVm)
                {
                    case EntityPropertyStringViewModel { EntityProperty: Entity.EntityProperty<string> sM } sVm
                        when sVm.EntityProperty != sM || sVm.Value != sM.Value:
                        sVm.Value = sM.Value;
                        break;
                    case EntityPropertyIoViewModel { EntityProperty: Entity.EntityProperty<EntityIo> ioM } ioVm
                        when ioVm.EntityProperty != ioM || ioVm.EntityProperty.Equals(ioM):
                        ioVm.TargetEntityName = ioM.Value?.TargetEntityName;
                        ioVm.Input = ioM.Value?.Input;
                        ioVm.Delay = ioM.Value?.Delay;
                        ioVm.Parameter = ioM.Value?.Parameter;
                        ioVm.TimesToFire = ioM.Value?.TimesToFire;
                        break;
                }
            }
        }

        // Not in EL, in ELVM -> remove from ELVM
        foreach (EntityViewModel entVm in Entities.Items)
        {
            if (!_entityLump.Data.Contains(entVm.Entity))
                removals.Add(entVm.Entity);
        }

        Entities.Edit(innerCache =>
        {
            innerCache.AddOrUpdate(additions);
            innerCache.Remove(removals);
        });

        foreach (EntityViewModel newEnt in additions)
            newEnt.MarkAsModified(); // Probably best to do this last
    }

    public void Dispose() => Entities.Dispose();
}
