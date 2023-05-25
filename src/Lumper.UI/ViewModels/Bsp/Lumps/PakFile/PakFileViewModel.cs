using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Lumps;
using Lumper.UI.Models;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

/// <summary>
///     ViewModel for the PakFile
/// </summary>
public class PakFileLumpViewModel : LumpBase
{
    //private readonly SourceList<PakFileEntryViewModel> _entries = new();
    private readonly PakFileEntryViewModel _entryRoot;
    public PakFileLumpViewModel(BspViewModel parent, PakFileLump pakFileLump)
        : base(parent)
    {
        _entryRoot = new PakFileEntryViewModel(this, pakFileLump.Zip);

        InitializeNodeChildrenObserver(_entryRoot._entries);
    }

    public override string NodeName => "PakFile";
}