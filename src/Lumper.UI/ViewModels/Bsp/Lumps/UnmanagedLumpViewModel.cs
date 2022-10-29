using Lumper.Lib.BSP.Lumps.BspLumps;

namespace Lumper.UI.ViewModels.Bsp.Lumps;

public class UnmanagedLumpViewModel : LumpBase
{
    public UnmanagedLumpViewModel(BspViewModel parent, BspLumpType lumpType) : base(parent)
    {
        NodeName = lumpType.ToString();
    }

    public override string NodeName { get; }
}