namespace Lumper.UI.ViewModels.VtfBrowser;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Lib.BSP.Lumps.BspLumps;
using Lib.BSP.Struct;
using Models.Matchers;
using Models.VTF;
using ReactiveUI;
using Services;

public partial class VtfBrowserViewModel : ViewModelBase
{
    public VtfBrowserViewModel()
    {
        if (!ActiveBspService.Instance.HasLoadedBsp)
            throw new InvalidOperationException();

        foreach (PakFileEntry? entry in ActiveBspService.Instance.BspFile!.GetLump<PakFileLump>().Entries
            .Where(x => x.Key.ToLower().EndsWith(".vtf")))
        {
            Observable.Start(() =>
                TextureBrowserItems.Add(new VtfBrowserItemViewModel(entry.Key, new VtfFile(entry))), RxApp.TaskpoolScheduler);
        }

        UpdateItems();
    }

    private double _dimensions = 128;
    public double Dimensions
    {
        get => _dimensions;
        set
        {
            this.RaiseAndSetIfChanged(ref _dimensions, value);
            this.RaisePropertyChanged(nameof(MaxNameWidth));
        }
    }

    // 8 is how much the text will be offset from the sides, so 4px left and 4px right
    public uint MaxNameWidth => (uint)Dimensions - 8;

    private bool _showCubemaps = false;
    public bool ShowCubemaps
    {
        get => _showCubemaps;
        set
        {
            this.RaiseAndSetIfChanged(ref _showCubemaps, value);
            UpdateItems();
        }
    }

    private string _textureSearch = "";
    public string TextureSearch
    {
        get => _textureSearch;
        set
        {
            this.RaiseAndSetIfChanged(ref _textureSearch, value);
            UpdateItems();
        }
    }

    private string _texturesCount = "";
    public string TexturesCount
    {
        get => _texturesCount;
        set => this.RaiseAndSetIfChanged(ref _texturesCount, value);
    }

    // Matches cubemap names which are formatted as cX_cY_cZ.vtf or cubemapdefault.vtf, including .hdr.vtf versions
    // X Y Z are the cubemap's origin
    // https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/utils/vbsp/cubemap.cpp
    [GeneratedRegex(@"^((c-?\d+_-?\d+_-?\d+)|cubemapdefault)(\.hdr){0,}\.vtf$")]
    private static partial Regex CubemapRegex();

    public ObservableCollection<VtfBrowserItemViewModel> TextureBrowserItems { get; } = [];

    private async void UpdateItems()
    {
        var isGlobPattern = TextureSearch.Contains('*') || TextureSearch.Contains('?');
        GlobMatcher matcher = isGlobPattern
            ? new GlobMatcher(TextureSearch, false, true)
            : new GlobMatcher($"*{TextureSearch}*", false, true);

        var count = 0;

        foreach (VtfBrowserItemViewModel item in TextureBrowserItems)
        {
            if (!_showCubemaps && CubemapRegex().IsMatch(item.Name))
            {
                item.IsVisible = false;
                continue;
            }

            item.IsVisible = string.IsNullOrWhiteSpace(TextureSearch) || await matcher.Match(item.Name);

            if (item.IsVisible)
                count++;
        }

        TexturesCount = $"{count} Items";
    }
}
