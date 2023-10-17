using System;
using System.IO;
using System.Linq;
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
    private readonly BspViewModel _parent;

    public BspNodeViewModel(BspViewModel parent)
        : base(parent)
    {
        _parent = parent;
        NodeName = Path.GetFileName(parent.BspFile.FilePath);
    }

    public override string NodeName
    {
        get;
    }

    public void Dispose()
    {
        _lumps.Dispose();
    }

    internal async Task InitializeAsync()
    {
        await Task.WhenAll(_parent.BspFile.Lumps.Select(lump => ParseLumpAsync(lump.Key, lump.Value)));
        InitializeNodeChildrenObserver(_lumps);
    }

    private async Task ParseLumpAsync(BspLumpType type, Lump<BspLumpType> lump)
    {
        await Task.Run(() =>
        {
            LumpBase lumpModel = lump switch
            {
                EntityLump el => new EntityLumpViewModel(BspView, el),
                PakFileLump el => new PakFileLumpViewModel(BspView, el),
            _ => new UnmanagedLumpViewModel(BspView, type)
            };
            _lumps.Add(lumpModel);
        });
    }

    protected override ValueTask<bool> Match(Matcher matcher,
        CancellationToken? cancellationToken)
    {
        return ValueTask.FromResult(true);
    }
}
