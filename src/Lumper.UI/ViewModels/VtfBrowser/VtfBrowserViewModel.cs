using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.UI.Models;
using Lumper.UI.Models.Matchers;
using ReactiveUI;

namespace Lumper.UI.ViewModels.VtfBrowser;

public partial class VtfBrowserViewModel : ViewModelBase
{
    public VtfBrowserViewModel(PakFileLump pakFileLump)
    {
        var entries = pakFileLump.Entries.Where(
            x => x.Key.ToLower().EndsWith(".vtf"));
        foreach (var entry in entries)
        {
            _textureBrowserItems.Add(new VtfBrowserItemViewModel(
            entry.Key, new VtfFileData(entry)));
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
    public uint MaxNameWidth => (uint)_dimensions - 8;

    private bool _showCubemaps = true;

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

    // matches cubemap names which are formatted as cX_cY_cZ.vtf or cubemapdefault.vtf, including .hdr.vtf versions
    // X Y Z are the cubemap's origin
    // https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/utils/vbsp/cubemap.cpp
    [GeneratedRegex(@"^((c-?\d+_-?\d+_-?\d+)|cubemapdefault)(\.hdr){0,}\.vtf$")]
    private static partial Regex _rgxCubemap();

    private ObservableCollection<VtfBrowserItemViewModel> _textureBrowserItems =
        new();

    public ObservableCollection<VtfBrowserItemViewModel> TextureBrowserItems => _textureBrowserItems;

    private void UpdateItems()
    {
        bool isGlobPattern = TextureSearch.Contains('*') || TextureSearch.Contains('?');
        var matcher = isGlobPattern
            ? new GlobMatcher(TextureSearch, false, true)
            : new GlobMatcher($"*{TextureSearch}*", false, true);

        int count = 0;

        foreach (var item in TextureBrowserItems)
        {
            if (!_showCubemaps && _rgxCubemap().IsMatch(item.Name))
            {
                item.IsVisible = false;
                continue;
            }

            item.IsVisible =  string.IsNullOrWhiteSpace(TextureSearch)
                   || matcher.Match(item.Name).Result;

            if (item.IsVisible) count++;
        }

        TexturesCount = $"{count} Items";
    }
}
