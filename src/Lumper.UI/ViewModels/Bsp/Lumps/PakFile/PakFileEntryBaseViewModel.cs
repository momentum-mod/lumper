using System.IO;
using System.Linq;
using System.Collections.Generic;
using DynamicData;
using SharpCompress.Archives.Zip;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public abstract class PakFileEntryBaseViewModel : BspNodeBase
{
    protected PakFileEntryBaseViewModel(BspNodeBase parent, string name)
        : base(parent)
    {
        _name = name;
    }
    public readonly SourceList<PakFileEntryBaseViewModel> _entries = new();
    protected readonly string _name;

    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"PakFileEntry{(string.IsNullOrWhiteSpace(_name) ? "" : $" ({_name})")}";

    public override bool IsModified =>
        Nodes is { Count: > 0 } && Nodes.Any(n => n.IsModified);

    protected void InitializeNodeChildrenObserver()
    {
        InitializeNodeChildrenObserver(_entries);
        foreach (var entry in _entries.AsObservableList().Items)
        {
            entry.InitializeNodeChildrenObserver();
        }
    }

    public virtual void Save(ZipArchive zip, ref List<Stream> streams)
    {
        foreach (var entry in _entries.Items)
        {
            entry.Save(zip, ref streams);
        }
    }


}