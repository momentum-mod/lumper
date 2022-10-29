using Lumper.Lib.BSP.Lumps.BspLumps;

namespace Lumper.UI.ViewModels.Bsp.Lumps;

public class UnmanagedLumpView : LumpBase
{
    public UnmanagedLumpView(BspViewModel parent, BspLumpType lumpType) : base(parent)
    {
        NodeName = lumpType.ToString();
    }

    public override string NodeName { get; }
    public override string NodeIcon => "/Assets/momentum-logo.png";
}