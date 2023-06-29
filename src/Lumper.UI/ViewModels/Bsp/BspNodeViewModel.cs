using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Lumps;
using Lumper.UI.Models;
using Lumper.UI.ViewModels.Bsp.Lumps.Entity;
using Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

namespace Lumper.UI.ViewModels.Bsp.Lumps;

/// <summary>
///     ViewModel for the top most BspNode
/// </summary>
public class BspNodeViewModel : BspNodeBase, IDisposable
{
    private readonly SourceList<LumpBase> _lumps = new();
    public BspNodeViewModel(BspViewModel parent)
        : base(parent)
    {
        NodeName = Path.GetFileName(parent.BspFile.FilePath);
        foreach (var (key, value) in parent.BspFile.Lumps)
            ParseLump(key, value);
        InitializeNodeChildrenObserver(_lumps);
    }

    public override string NodeName
    {
        get;
    }

    public void Dispose()
    {
        _lumps.Dispose();
    }

    private void ParseLump(BspLumpType type, Lump<BspLumpType> lump)
    {
        LumpBase lumpModel = lump switch
        {
            EntityLump el => new EntityLumpViewModel(BspView, el),
            PakFileLump el => new PakFileLumpViewModel(BspView, el),
            _ => new UnmanagedLumpViewModel(BspView, type)
        };
        _lumps.Add(lumpModel);
    }

    protected override ValueTask<bool> Match(Matcher matcher,
        CancellationToken? cancellationToken)
    {
        return ValueTask.FromResult(true);
    }
}
