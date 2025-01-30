namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public abstract class PakfileEntryViewModel : HierarchicalBspNode
{
    // The non-reactive BSP.Lib entry this class wraps.
    public PakfileEntry BaseEntry { get; }

    [Reactive]
    public string Key { get; set; }

    public ReadOnlySpan<byte> Data => BaseEntry.GetData();

    [ObservableAsProperty]
    public string Name { get; } = "";

    [ObservableAsProperty]
    public string Extension { get; } = null!;

    public long? CompressedSize => BaseEntry.CompressedSize;

    [ObservableAsProperty]
    public string? Hash { get; }
    protected PakfileEntryViewModel(PakfileEntry baseEntry, BspNode parent)
        : base(parent)
    {
        BaseEntry = baseEntry;
        Key = BaseEntry.Key;

        IObservable<string> key = this.WhenAnyValue(x => x.Key);
        key.BindTo(this, x => x.BaseEntry.Key);
        key.Select(x => new FileInfo(x).Name).ToPropertyEx(this, x => x.Name);
        key.Select(x => new FileInfo(x).Extension.ToLower()).ToPropertyEx(this, x => x.Extension);

        // Hash access causes a pakfile read (expensive, single-threaded!) so defer until requested,
        // and never run in the main (UI) thread!
        this.WhenAnyValue(x => x.BaseEntry)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(x => x.Hash)
            .ToPropertyEx(this, x => x.Hash, deferSubscription: true);
    }

    public abstract void Load(CancellationTokenSource? cts = null);

    public override void MarkAsModified()
    {
        base.MarkAsModified();
        BaseEntry.IsModified = true;
    }

    public void UpdateData(ReadOnlyMemory<byte> data)
    {
        BaseEntry.UpdateData(data);
        this.RaisePropertyChanged(nameof(Data));
        this.RaisePropertyChanged(nameof(Hash));
    }

    /// <summary>
    /// Called during save, only needed for viewmodels that store their contents separately from the underlying model,
    /// such as text viewmodels.
    ///
    /// For viewmodels that store their contents on the model (this is important if we care about hashing that
    /// information), this can be a noop.
    ///
    /// <see cref="BspNode">See BspNode comments for architectural overview</see>
    /// </summary>
    public override void UpdateModel() { }
}
