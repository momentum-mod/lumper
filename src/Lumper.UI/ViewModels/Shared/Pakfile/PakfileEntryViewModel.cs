namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
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

    protected MemoryStream DataStream
    {
        get => BaseEntry.GetReadOnlyStream();
        set
        {
            BaseEntry.UpdateData(value);
            this.RaisePropertyChanged();
        }
    }

    [ObservableAsProperty]
    public string Name { get; } = "";

    [ObservableAsProperty]
    public string Extension { get; } = null!;

    [Reactive]
    public string? Hash { get; private set; }

    [Reactive]
    public bool MatchesOfficialAsset { get; private set; }

    [Reactive]
    public List<AssetManifest.Asset>? OfficialAssets { get; private set; }

    public long? CompressedSize => BaseEntry.CompressedSize;

    protected PakfileEntryViewModel(PakfileEntry baseEntry, BspNode parent)
        : base(parent)
    {
        BaseEntry = baseEntry;
        Key = BaseEntry.Key;

        IObservable<string> key = this.WhenAnyValue(x => x.Key);
        key.BindTo(this, x => x.BaseEntry.Key);
        key.Select(x => new FileInfo(x).Name).ToPropertyEx(this, x => x.Name);
        key.Select(x => new FileInfo(x).Extension.ToLower()).ToPropertyEx(this, x => x.Extension);
    }

    public abstract void Load(CancellationTokenSource? cts = null);

    public override void MarkAsModified()
    {
        base.MarkAsModified();
        BaseEntry.IsModified = true;
    }

    protected void UpdateHash()
    {
        Hash = BaseEntry.HashSHA1;
        UpdateHashProperties();
    }

    protected void UpdateHash(string value)
    {
        Hash = BitConverter.ToString(SHA1.HashData(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        UpdateHashProperties();
    }

    private void UpdateHashProperties()
    {
        if (AssetManifest.Manifest.TryGetValue(Hash!, out List<AssetManifest.Asset>? assets))
        {
            MatchesOfficialAsset = true;
            OfficialAssets = assets;
        }
        else
        {
            MatchesOfficialAsset = false;
            OfficialAssets = null;
        }
    }
}
