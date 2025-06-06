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

public sealed class EntityLumpViewModel : LumpViewModel
{
    private readonly EntityLump _entityLump;

    public SourceCache<EntityViewModel, int> Entities { get; } = new(ent => ent.Entity.GetHashCode());

    [ObservableAsProperty]
    public int EntityCount { get; }

    public bool IsEditingStream { get; set; }

    // Don't want to have to include this but can't do saving without :(
    public RawEntitiesViewModel? RawEntitiesViewModel { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Required by BspService.LazyLoadLump
    public EntityLumpViewModel()
    {
        throw new NotImplementedException();
    }

    // This class is created the first time something requests this lump from BspService,
    // and discarded when that BSP file is closed.
    public EntityLumpViewModel(BspFile bsp)
    {
        BspService.Instance.ThrowIfNoLoadedBsp();

        _entityLump =
            bsp.GetLump<EntityLump>()
            ?? throw new InvalidOperationException("BSP file does not contain an Entity lump somehow!");

        LoadEntityList();

        Entities.CountChanged.ObserveOn(RxApp.MainThreadScheduler).ToPropertyEx(this, x => x.EntityCount);
    }

    private void LoadEntityList()
    {
        Entities.Edit(innerCache =>
        {
            innerCache.Clear();
            innerCache.AddOrUpdate(_entityLump.Data.Select(ent => new EntityViewModel(ent, this)));
        });
    }

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

    public void RemoveMultiple(IEnumerable<EntityViewModel> entities)
    {
        Entities.Edit(innerCache =>
        {
            foreach (EntityViewModel entity in entities)
            {
                _entityLump.Data.Remove(entity.Entity);
                innerCache.Remove(entity);
            }
        });
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

    public override void PushChangesToModel()
    {
        // Entity property viewmodel setters set values on underlying models
        if (IsEditingStream && RawEntitiesViewModel is not null)
            RawEntitiesViewModel.SaveOrDiscardEntityLump();
    }

    public override void PullChangesFromModel()
    {
        List<EntityViewModel> additions = [];
        List<Entity> removals = [];

        // Pretty hefty iterations here fortunately since this class and EntityLump use a SourceCache and HashSet
        // respectively, with a Entity-based key, we get constant-time lookup. This can handle a map with 10,000
        // entities in ~25ms, whilst List-based version was ~3s.
        foreach (Entity entM in _entityLump.Data)
        {
            // In EL, not in ELVM -> add to ELVM
            Optional<EntityViewModel> entVmLookup = Entities.Lookup(entM.GetHashCode());
            if (!entVmLookup.HasValue)
            {
                var newEntity = new EntityViewModel(entM, this);
                additions.Add(newEntity);
                continue;
            }

            EntityViewModel entVm = entVmLookup.Value;

            // Remove old properties
            foreach (
                EntityPropertyViewModel propVm in entVm
                    .Properties.ToList()
                    .Where(propVm => entM.Properties.All(x => x.Key != propVm.Key))
            )
            {
                entVm.Properties.Remove(propVm);
                entVm.MarkAsModified();
            }

            // Iterating over model properties not viewmodel and searching by key, for all we know a Job could have
            // removed and recreated a property, can't rely on EntityPropertyViewModel.Property referring to the
            // right instance.
            foreach (Entity.EntityProperty propM in entM.Properties)
            {
                EntityPropertyViewModel? propVm = null;
                var props = entVm.Properties.Where(x => x.Key == propM.Key).ToList();
                if (props.Count == 1)
                {
                    propVm = props[0];
                }
                else if (props.Count > 1)
                {
                    // Model can potentially have multiple properties with same key, in that case
                    // pick based on index.
                    int idx = entM.Properties.Where(x => x.Key == propM.Key).ToList().IndexOf(propM);
                    if (idx < props.Count)
                        propVm = props[idx];
                }

                if (propVm is not null)
                {
                    if (propVm is EntityPropertyStringViewModel strVm)
                    {
                        if (propM is Entity.EntityProperty<string> strM)
                        {
                            // Setters here call RaisePropertyChanged, MarkAsModified
                            strVm.Value = strM.Value;
                        }
                        else
                        {
                            // Weird case, we became EntityIO - create new viewmodel from scratch.
                            // Careful not to use EntityViewModel.CreateProperty here, which also creates new models.
                            entVm.Properties.Remove(propVm);
                            entVm.Properties.Add(EntityPropertyViewModel.Create(propM, entVm));
                            entVm.MarkAsModified();
                        }
                    }
                    else if (propVm is EntityPropertyIoViewModel ioVm)
                    {
                        if (propM is Entity.EntityProperty<EntityIo> ioM)
                        {
                            ioVm.TargetEntityName = ioM.Value.TargetEntityName;
                            ioVm.Input = ioM.Value.Input;
                            ioVm.Parameter = ioM.Value.Parameter;
                            ioVm.Delay = ioM.Value.Delay;
                            ioVm.TimesToFire = ioM.Value.TimesToFire;
                        }
                        else
                        {
                            entVm.Properties.Remove(propVm);
                            entVm.Properties.Add(EntityPropertyViewModel.Create(propM, entVm));
                            entVm.MarkAsModified();
                        }
                    }
                }
                else
                {
                    // Add new properties
                    entVm.Properties.Add(EntityPropertyViewModel.Create(propM, entVm));
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
            innerCache.Remove(removals.Select(x => x.GetHashCode()));
        });

        foreach (EntityViewModel newEnt in additions)
            newEnt.MarkAsModified(); // Probably best to do this last
    }

    public override void Dispose()
    {
        Entities.Dispose();
    }
}
