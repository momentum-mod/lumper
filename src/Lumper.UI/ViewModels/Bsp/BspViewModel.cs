using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.UI.Models;
using Lumper.UI.ViewModels.Bsp.Lumps;

namespace Lumper.UI.ViewModels.Bsp;

public class BspViewModel : BspNodeBase
{
    private readonly SourceList<LumpBase> _lumps = new();

    public BspViewModel(BspFile bspFile)
    {
        NodeName = Path.GetFileName(bspFile.FilePath);
        foreach (var (key, value) in bspFile.Lumps)
            ParseLump(key, value);
        InitializeNodeChildrenObserver(_lumps);
    }


    public override string NodeName { get; }
    public override string NodeIcon => "/Assets/momentum-logo.png";

    private void ParseLump(BspLumpType type, Lump<BspLumpType> lump)
    {
        var lumpModel = type switch
        {
            _ => new UnmanagedLumpView(this, type)
        };
        _lumps.Add(lumpModel);
    }

    protected override async Task<bool> Match(Matcher matcher, CancellationToken? cancellationToken)
    {
        return true;
    }
}