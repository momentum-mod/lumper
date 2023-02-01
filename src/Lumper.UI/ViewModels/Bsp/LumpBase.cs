namespace Lumper.UI.ViewModels.Bsp;

/// <summary>
///     ViewModel base for all <see cref="Lumper.Lib.BSP.Lumps.Lump" /> instances.
/// </summary>
public abstract class LumpBase : BspNodeBase
{
    public LumpBase(BspViewModel parent)
        : base(parent)
    {
    }
}
