using Lumper.Lib.BSP.Lumps.BspLumps;

namespace Lumper.UI.ViewModels.Bsp.Lumps;

/// <summary>
///     ViewModel for not yet implemented lump types
/// </summary>
public class UnmanagedLumpViewModel : LumpBase
{
    public UnmanagedLumpViewModel(BspViewModel parent, BspLumpType lumpType)
        : base(parent)
    {
        NodeName = lumpType.ToString();
    }

    public override string NodeName
    {
        get;
    }
}
