namespace Lumper.UI.ViewModels.Bsp.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;

/// <summary>
///     ViewModel for not yet implemented lump types
/// </summary>
public class UnmanagedLumpViewModel(BspViewModel parent, BspLumpType lumpType) : LumpBase(parent)
{
    public override string NodeName
    {
        get;
    } = lumpType.ToString();
}
