namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.Util;
using ReactiveUI.Fody.Helpers;

public abstract class PakfileEntryViewModel : HierarchicalBspNode
{
    // The non-reactive BSP.Lib entry this class wraps.
    public PakfileEntry BaseEntry { get; }

    private string _key = "";
    public string Key
    {
        get => _key;
        set
        {
            _key = value;
            BaseEntry.Key = value;
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

    public abstract void Load(CancellationTokenSource? cts = null);

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
        ReadOnlySpan<byte> data = BaseEntry.GetData();
        OnDataUpdate();
        return data;
    }

    private void OnDataUpdate()
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
    ///
    /// <see cref="BspNode">See BspNode comments for architectural overview</see>
    /// </summary>
    public override void UpdateModel() { }
}
