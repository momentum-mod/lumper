using DynamicData;
using Lumper.Lib.BSP.Lumps.BspLumps;

namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;

public class EntityLumpViewModel : LumpBase
{
    private readonly SourceList<EntityViewModel> _entities = new();

    public EntityLumpViewModel(BspViewModel parent, EntityLump entityLump) : base(parent)
    {
        foreach (var entity in entityLump.Data)
            _entities.Add(new EntityViewModel(this, entity));

        InitializeNodeChildrenObserver(_entities);
    }

    public override string NodeName => "Entity Group";
}