namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using SharpCompress.Archives.Zip;

public abstract class PakFileEntryBaseViewModel : BspNodeBase, IDisposable
{
    protected PakFileEntryBaseViewModel(BspNodeBase parent, string name)
        : base(parent)
    {
        _name = name;
        Path = GetPath();
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
            InitializeNodeChildrenObserver(_entries);
        }
    }
    public readonly SourceList<PakFileEntryBaseViewModel> _entries = new();

    private string _name = "";
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _path = "";
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
        List<string> path = [];
        GetPath(ref path, true);
        path.Reverse();
        var ret = string.Join("/", path);
        if (ret.Length != 0)
            ret += "/";
        return ret;
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
        foreach (PakFileEntryBaseViewModel entry in _entries.Items)
        {
            entry.Save(zip, ref streams);
        }
    }

    public void Dispose() => throw new NotImplementedException();
}
