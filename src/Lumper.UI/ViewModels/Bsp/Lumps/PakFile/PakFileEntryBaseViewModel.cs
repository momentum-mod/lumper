using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData;
using SharpCompress.Archives.Zip;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public abstract class PakFileEntryBaseViewModel : BspNodeBase
{
    protected PakFileEntryBaseViewModel(BspNodeBase parent, string name)
        : base(parent)
    {
        _name = name;
        this.WhenAnyValue(x => x.Name)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(m => m is not null)
            .Subscribe(_ =>
                this.RaisePropertyChanged(nameof(NodeName)));

        if (parent is PakFileEntryBranchViewModel branch)
        {
            branch.WhenAnyValue(x => x.Name)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Where(m => m is not null)
                .Subscribe(_ =>
                   Path = GetPath());
        }
        InitializeNodeChildrenObserver(_entries);
    }
    public readonly SourceList<PakFileEntryBaseViewModel> _entries = new();

    private string _name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _path;
    public string Path
    {
        get => _path;
        protected set => this.RaiseAndSetIfChanged(ref _path, value);
    }


    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"PakFileEntry{(string.IsNullOrWhiteSpace(_name) ? "" : $" ({_name})")}";

    public override bool IsModified =>
        Nodes is { Count: > 0 } && Nodes.Any(n => n.IsModified);

    protected string GetPath()
    {
        List<string> path = new();
        GetPath(ref path, true);
        path.Reverse();
        return string.Join("/", path) + "/";
    }

    protected void GetPath(ref List<string> path, bool skip = false)
    {
        if (Parent is PakFileEntryBranchViewModel branch)
        {
            if (!skip)
                path.Add(Name);
            branch.GetPath(ref path);
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
