namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lumper.Lib.AssetManifest;
using Lumper.Lib.Bsp.Struct;
using ReactiveUI.Fody.Helpers;

public abstract class PakfileEntryViewModel : HierarchicalBspNode
{
    // The non-reactive BSP.Lib entry this class wraps.
    public PakfileEntry BaseEntry { get; }

    private string _key = "";
    public string Key
    {
        get => _key;
        private set
        {
            _key = value;
            var fi = new FileInfo(value);
            // We often load hundreds of these at once, bad for perf to WhenAnyValue => ToProperty observables for
            // each - just use setters and [Reactive] RaiseAndSetIfChanged.
            Name = fi.Name;
            Extension = fi.Extension;
        }
    }

    [Reactive]
    public string Name { get; set; } = "";

    [Reactive]
    public string Extension { get; set; } = "";

    public long? CompressedSize => BaseEntry.CompressedSize;

    public long? UncompressedSize => BaseEntry.UncompressedSize;

    [Reactive]
    public string? Hash { get; set; }

    [Reactive]
    public List<AssetManifest.Asset> MatchingGameAssets { get; set; } = [];

    protected PakfileEntryViewModel(PakfileEntry baseEntry, BspNode parent)
        : base(parent)
    {
        BaseEntry = baseEntry;
        Key = BaseEntry.Key;
    }

    public virtual void Load(CancellationTokenSource? cts = null) { }

    public static PakfileEntryViewModel Create(PakfileEntry entry, PakfileLumpViewModel parent)
    {
        return Path.GetExtension(entry.Key).Equals(".vtf", StringComparison.OrdinalIgnoreCase)
            ? new PakfileEntryVtfViewModel(entry, parent)
            // Treat anything that isn't a VTF as a text file. If it's not a file we recognise as text,
            // the UI shows a warning, but does allow editing -- may be some file extensions that we
            // don't recognise but are still text.
            : new PakfileEntryTextViewModel(entry, parent);
    }

    public void Rename(string newKey)
    {
        var pakfileLump = (PakfileLumpViewModel)Parent;
        pakfileLump.Entries.Edit(updater =>
        {
            updater.Remove(Key);
            Key = newKey;
            updater.AddOrUpdate(this);
        });

        BaseEntry.Rename(newKey);
        MarkAsModified();
    }

    public override void MarkAsModified()
    {
        base.MarkAsModified();
        BaseEntry.IsModified = true;
    }

    public void UpdateData(ReadOnlyMemory<byte> data)
    {
        BaseEntry.UpdateData(data);
        OnDataUpdate();
    }

    public void PrefetchData()
    {
        BaseEntry.PrefetchData();
        OnDataUpdate();
    }

    public ReadOnlySpan<byte> GetData()
    {
        string? oldHash = BaseEntry.HasLoadedData ? Hash : null;

        ReadOnlySpan<byte> data = BaseEntry.GetData();

        if (BaseEntry.Hash != oldHash)
            OnDataUpdate();

        return data;
    }

    public void CopyKeyToClipboard()
    {
        _ = Program.MainWindow.Clipboard?.SetTextAsync(Key);
    }

    public void RemoveFromPakfile()
    {
        ((PakfileLumpViewModel)Parent).DeleteEntry(this);
    }

    /// <summary>
    /// Called whenever unique new data is loaded by viewmodel code. Note this is *not* called the if BaseEntry is
    /// modified by non-viewmodel code, e.g. during Jobs. If a job has potentially modified the entry, you need to call
    /// UpdateViewModelFromModel() on the Pakfile lump to detect changes.
    /// </summary>
    public virtual void OnDataUpdate()
    {
        // Hashes need to read the entire ZipArchive contents to be calculated, and we can only read one zip entry at a
        // time. If we use a getter that calls GetData() we massively degrade performance in the texture browser, since
        // every <ItemsControl> template requests a hash which requests GetData(), which gets stuck behind a lock
        // as archive entries are read sequentially.
        //
        // Instead, we let GetData() calls set our hash, and in the texture browser, let the VTF loading loop be
        // responsible for GetData() calls, which will set Hash when once returned, which will reactivity update in
        // the UI.
        //
        // If you need calculate Hash or other values derived from zip entry data, call PrefetchData(). Just be wary
        // of running for multiple pakfileentryvms in parallel!
        Hash = BaseEntry.Hash;
        MatchingGameAssets = Hash is not null ? AssetManifest.Manifest.GetValueOrDefault(Hash) ?? [] : [];
    }

    /// <summary>
    /// Called during save, only needed for viewmodels that store their contents separately from the underlying model,
    /// such as text viewmodels.
    ///
    /// For viewmodels that store their contents on the model (this is important if we care about hashing that
    /// information), this can be a noop.
    /// </summary>
    public override void PushChangesToModel() { }
}
