namespace Lumper.UI.ViewModels.Pages.VtfBrowser;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using DynamicData;
using Models.Matchers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;
using Shared.Pakfile;

public partial class VtfBrowserViewModel : ViewModel
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

    private readonly IObservable<IReadOnlyCollection<PakfileEntryVtfViewModel>> _items =
        BspService.Instance
            .WhenAnyValue(x => x.PakfileLumpViewModel)
            .Where(x => x is not null)
            .SelectMany(x => x!
                .Entries
                .Connect()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Filter(y => y is PakfileEntryVtfViewModel)
                .Transform(vtfVm =>
                {
                    vtfVm.Load();
                    return (PakfileEntryVtfViewModel)vtfVm;
                })
                .ToCollection()
            );

    public VtfBrowserViewModel()
    {
        _items.Select(x => x.Count).ToPropertyEx(this, x => x.TotalItems);
        _items.Select(x => x.Count == 0).ToPropertyEx(this, x => x.IsEmpty);

        this.WhenAnyValue(x => x.TextureSearch)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .CombineLatest(this.WhenAnyValue(x => x.ShowCubemaps))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .CombineLatest(_items)
            .Select(x =>
            {
                ((var search, var showCubemaps), IReadOnlyCollection<PakfileEntryVtfViewModel> items) = x;
                GlobMatcher matcher = search.Contains('*') || search.Contains('?')
                    ? new GlobMatcher(search, false, true)
                    : new GlobMatcher($"*{search}*", false, true);

                // Can appear to be stupid slow in the UI, but this stuff is fine, it's Avalonia render stuff.
                return items
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
