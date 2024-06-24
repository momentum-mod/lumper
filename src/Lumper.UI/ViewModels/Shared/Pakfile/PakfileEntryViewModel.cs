namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.IO;
using System.Reactive.Linq;
using Lib.BSP.Struct;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public abstract class PakfileEntryViewModel : HierarchicalBspNode
{
    // The non-reactive BSP.Lib entry this class wraps.
    public PakfileEntry BaseEntry { get; }

    [Reactive]
    public string Key { get; set; }

    protected MemoryStream DataStream
    {
        get => BaseEntry.GetReadOnlyStream();
        set
        {
            BaseEntry.UpdateData(value.ToArray());
            this.RaisePropertyChanged();
        }
    }

    [ObservableAsProperty]
    public string Name { get; } = "";

    [ObservableAsProperty]
    public string Extension { get; } = null!;

    public long? CompressedSize => BaseEntry.CompressedSize;

    protected PakfileEntryViewModel(PakfileEntry baseEntry)
    {
        BaseEntry = baseEntry;
        Key = BaseEntry.Key;

        IObservable<string> key = this.WhenAnyValue(x => x.Key);
        key.BindTo(this, x => x.BaseEntry.Key);
        key.Select(x => new FileInfo(x).Name).ToPropertyEx(this, x => x.Name);
        key.Select(x => new FileInfo(x).Extension.ToLower()).ToPropertyEx(this, x => x.Extension);
    }

    public abstract void Load();

    public override void MarkAsModified()
    {
        base.MarkAsModified();
        BaseEntry.IsModified = true;
    }
}
