using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SharpCompress.Archives.Zip;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public class PakFileEntryLeafViewModel : PakFileEntryBaseViewModel
//, IDisposable
{
    protected readonly PakFileEntry? _entry;
    public PakFileEntry? Entry { get => _entry; }

    public PakFileEntryLeafViewModel(PakFileEntryBranchViewModel parent,
        PakFileEntry entry, string name)
        : base(parent, name)
    {
        _entry = entry;
    }
}
