using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using Lumper.Lib.BSP.Struct;



namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public class PakFileEntryLeafViewModel : PakFileEntryBaseViewModel
{
    protected readonly PakFileEntry _entry;
    public PakFileEntry Entry { get => _entry; }
    public string Extension { get => new FileInfo(Name).Extension.ToLower(); }

    public PakFileEntryLeafViewModel(PakFileEntryBranchViewModel parent,
        PakFileEntry entry, string name)
        : base(parent, name)
    {
        _entry = entry;
        Path = GetPath();
        this.WhenAnyValue(x => x.Name)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(m => m is not null)
            .Subscribe(_ =>
                {
                    Entry.Key = Path + Name;
                    this.RaisePropertyChanged(nameof(Extension));
                }
                );
        this.WhenAnyValue(x => x.Path)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(m => m is not null)
            .Subscribe(_ =>
            {
                string newKey = Path + Name;
                if (newKey != Entry.Key)
                {
                    Entry.Key = newKey;
                    if (Parent is PakFileEntryBranchViewModel branch)
                    {
                        var dir = branch.CreatePathRoot(Entry.Key, out _);
                        dir.MoveNode(this);
                    }
                }
            });
    }

    public void Delete()
    {
        if (Parent is PakFileEntryBranchViewModel branch)
            branch.Delete(this);
    }

    public void OpenTab()
    {
        BspView.Open(this);
    }

}
