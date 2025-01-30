namespace Lumper.UI.ViewModels.Pages.VtfBrowser;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using DynamicData;
using Lumper.Lib.ExtensionMethods;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Pakfile;
using Lumper.UI.Views.Pages.VtfBrowser;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public partial class VtfBrowserViewModel : ViewModelWithView<VtfBrowserViewModel, VtfBrowserView>
{
    public StateService StateService { get; } = StateService.Instance;

    [ObservableAsProperty]
    public ReadOnlyCollection<PakfileEntryVtfViewModel>? FilteredItems { get; }

    [ObservableAsProperty]
    public int TotalItems { get; set; }

    [Reactive]
    public string TextureSearch { get; set; } = "";

    public VtfBrowserViewModel()
    {
        // Generate observable catch of just VTFs, loading them as they're added
        IObservableCache<PakfileEntryVtfViewModel, string> vtfs = BspService
            .Instance.WhenAnyValue(x => x.PakfileLumpViewModel)
            .Where(x => x is not null)
            .Select(x =>
                x!
                    .Entries.Connect()
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Filter(y => y is PakfileEntryVtfViewModel)
                    .Cast(y => (PakfileEntryVtfViewModel)y)
            )
            .Switch()
            .AsObservableCache();

        vtfs.CountChanged.ToPropertyEx(this, x => x.TotalItems);

        this.WhenAnyValue(x => x.TextureSearch)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .CombineLatest(this.WhenAnyValue(x => x.StateService.VtfBrowserShowCubemaps))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .CombineLatest(vtfs.Connect())
            .Select(x =>
            {
                ((string search, bool showCubemaps), IChangeSet<PakfileEntryVtfViewModel, string> _) = x;

                // Can appear to be stupid slow in the UI, but this stuff is fine, it's Avalonia render stuff.
                return vtfs
                    .Items.Where(item =>
                        (showCubemaps || !CubemapRegex().IsMatch(item.Name))
                        && (string.IsNullOrWhiteSpace(search) || search.MatchesSimpleExpression(item.Name))
                    )
                    .OrderBy(y => y.Name)
                    .ToList()
                    .AsReadOnly();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.FilteredItems);

        var cts = new CancellationTokenSource();
        PageService
            .Instance.WhenAnyValue(x => x.ActivePage)
            .Where(x => x is Page.VtfBrowser)
            .Select(_ =>
                this.WhenAnyValue(x => x.FilteredItems)
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .WhereNotNull()
                    .Do(items =>
                    {
                        foreach (PakfileEntryVtfViewModel item in items.Where(x => !x.Loaded))
                            item.Load(cts);
                    })
                    .TakeUntil(
                        PageService
                            .Instance.WhenAnyValue(x => x.ActivePage)
                            .Where(x => x is not Page.VtfBrowser)
                            .Do(_ =>
                            {
                                cts.Cancel();
                                cts = new CancellationTokenSource();
                            })
                    )
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
