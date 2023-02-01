using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using DynamicData;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.UI.Models;
using Lumper.UI.ViewModels.Bsp.Lumps;
using Lumper.UI.ViewModels.Bsp.Lumps.Entity;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp;

/// <summary>
///     View model for <see cref="Lumper.Lib.BSP.BspFile" />
/// </summary>
public class BspViewModel : BspNodeBase, IDisposable
{
    private readonly SourceList<LumpBase> _lumps = new();
    private string? _filePath;

    public BspViewModel(MainWindowViewModel main, BspFile bspFile)
        : base(main)
    {
        BspFile = bspFile;
        NodeName = Path.GetFileName(bspFile.FilePath);
        foreach (var (key, value) in bspFile.Lumps)
            ParseLump(key, value);
        InitializeNodeChildrenObserver(_lumps);
    }

    public BspFile BspFile
    {
        get;
    }

    public override string NodeName
    {
        get;
    }

    public string? FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    public void Dispose()
    {
        _lumps.Dispose();
    }

    private void ParseLump(BspLumpType type, Lump<BspLumpType> lump)
    {
        LumpBase lumpModel = lump switch
        {
            EntityLump el => new EntityLumpViewModel(this, el),
            _ => new UnmanagedLumpViewModel(this, type)
        };
        _lumps.Add(lumpModel);
    }

    protected override ValueTask<bool> Match(Matcher matcher,
        CancellationToken? cancellationToken)
    {
        return ValueTask.FromResult(true);
    }
}
