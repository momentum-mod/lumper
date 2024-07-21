namespace Lumper.UI.ViewModels.Pages.VtfBrowser;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DynamicData;
using Models.Matchers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;
using Shared.Pakfile;
using Views.Pages.VtfBrowser;

public partial class VtfBrowserViewModel : ViewModelWithView<VtfBrowserViewModel, VtfBrowserView>
{
    [ObservableAsProperty]
    public ReadOnlyCollection<PakfileEntryVtfViewModel>? FilteredItems { get; }

    [ObservableAsProperty]
    public int TotalItems { get; set; }

    [ObservableAsProperty]
    public bool IsEmpty { get; set; }

    // Using a power of 2 doesn't have a significant improvement visually and 128/256 sizes feel too small/large
    [Reactive]
    public double Dimensions { get; set; } = 192;

    [Reactive]
    public bool ShowCubemaps { get; set; }

    [Reactive]
    public string TextureSearch { get; set; } = "";

    [Reactive]
    private ReadOnlyCollection<PakfileEntryVtfViewModel> _itemsWtf { get; set; }

    [Reactive]
    private IObservableCache<PakfileEntryVtfViewModel, string> items2 { get; set; }

    public VtfBrowserViewModel()
    {
        // TODO: WHY IS IT BACKWARDS???
        items2 = BspService.Instance
            .WhenAnyValue(x => x.PakfileLumpViewModel)
            .Select(x =>
                x!.Entries
                    .Connect()
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Filter(y => y is PakfileEntryVtfViewModel)
                    // TODO: Why cant we use .Do?
                    .Transform(vtfVm =>
                    {
                        vtfVm.Load();
                        return (PakfileEntryVtfViewModel)vtfVm;
                    })
            ).Switch().AsObservableCache();

        this.WhenAnyValue(x => x.items2).Select(x => x.CountChanged).Switch().ToPropertyEx(this, x => x.TotalItems);
        this.WhenAnyValue(x => x.items2).Select(x => x.CountChanged.Select(y => y == 0)).Switch()
            .ToPropertyEx(this, x => x.IsEmpty);

        this.WhenAnyValue(x => x.TextureSearch)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .CombineLatest(this.WhenAnyValue(x => x.ShowCubemaps))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .CombineLatest(this.WhenAnyValue(x => x.items2).Select(x => x.Connect()).Switch())
            .Select(x =>
            {
                var ((search, showCubemaps), _) = x;
                GlobMatcher matcher = search.Contains('*') || search.Contains('?')
                    ? new GlobMatcher(search, false, true)
                    : new GlobMatcher($"*{search}*", false, true);

                // Can appear to be stupid slow in the UI, but this stuff is fine, it's Avalonia render stuff.
                return items2.Items
                    .Where(item =>
                        (showCubemaps || !CubemapRegex().IsMatch(item.Name)) &&
                        (string.IsNullOrWhiteSpace(search) || matcher.Match(item.Name))
                    )
                    .ToList()
                    .AsReadOnly();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.FilteredItems);
    }

    // Matches cubemap names which are formatted as cX_cY_cZ.vtf or cubemapdefault.vtf, including .hdr.vtf versions
    // X Y Z are the cubemap's origin
    // https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/utils/vbsp/cubemap.cpp
    [GeneratedRegex(@"^((c-?\d+_-?\d+_-?\d+)|cubemapdefault)(\.hdr){0,}\.vtf$")]
    private static partial Regex CubemapRegex();
}
