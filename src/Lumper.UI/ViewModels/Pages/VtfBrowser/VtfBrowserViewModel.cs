namespace Lumper.UI.ViewModels.Pages.VtfBrowser;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
    public int NumberOfOfficialAssets { get; set; }

    // Using a power of 2 doesn't have a significant improvement visually and 128/256 sizes feel too small/large
    [Reactive]
    public double Dimensions { get; set; } = 192;

    [Reactive]
    public bool ShowCubemaps { get; set; }

    [Reactive]
    public string TextureSearch { get; set; } = "";

    public VtfBrowserViewModel()
    {
        // Generate observable catch of just VTFs, loading them as they're added
        IObservableCache<PakfileEntryVtfViewModel, string> vtfs = BspService.Instance
            .WhenAnyValue(x => x.PakfileLumpViewModel)
            .Where(x => x is not null)
            .Select(x => x!.Entries
                .Connect()
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Filter(y => y is PakfileEntryVtfViewModel)
                .Cast(y => (PakfileEntryVtfViewModel)y)
            )
            .Switch()
            .AsObservableCache();

        vtfs.CountChanged.ToPropertyEx(this, x => x.TotalItems);

        this.WhenAnyValue(x => x.TextureSearch)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .CombineLatest(this.WhenAnyValue(x => x.ShowCubemaps))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .CombineLatest(vtfs.Connect())
            .Select(x =>
            {
                var ((search, showCubemaps), _) = x;
                GlobMatcher matcher = search.Contains('*') || search.Contains('?')
                    ? new GlobMatcher(search, false, true)
                    : new GlobMatcher($"*{search}*", false, true);

                // Can appear to be stupid slow in the UI, but this stuff is fine, it's Avalonia render stuff.
                return vtfs.Items
                    .Where(item =>
                        (showCubemaps || !CubemapRegex().IsMatch(item.Name)) &&
                        (string.IsNullOrWhiteSpace(search) || matcher.Match(item.Name))
                    )
                    .OrderBy(y => y.Name)
                    .ToList()
                    .AsReadOnly();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.FilteredItems);


        var cts = new CancellationTokenSource();
        PageService.Instance.WhenAnyValue(x => x.ActivePage)
            .Where(x => x is Page.VtfBrowser)
            .Select(_ => this.WhenAnyValue(x => x.FilteredItems)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .WhereNotNull()
                .Do(items =>
                {
                    foreach (PakfileEntryVtfViewModel item in items.Where(x => !x.Loaded))
                        item.Load(cts);
                })
                .TakeUntil(PageService.Instance.WhenAnyValue(x => x.ActivePage)
                    .Where(x => x is not Page.VtfBrowser)
                    .Do(_ =>
                    {
                        cts.Cancel();
                        cts = new CancellationTokenSource();
                    }))
            )
            .Switch()
            .Subscribe();
    }

    // Matches cubemap names which are formatted as cX_cY_cZ.vtf or cubemapdefault.vtf, including .hdr.vtf versions
    // X Y Z are the cubemap's origin
    // https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/utils/vbsp/cubemap.cpp
    [GeneratedRegex(@"^((c-?\d+_-?\d+_-?\d+)|cubemapdefault)(\.hdr){0,}\.vtf$")]
    private static partial Regex CubemapRegex();
}
