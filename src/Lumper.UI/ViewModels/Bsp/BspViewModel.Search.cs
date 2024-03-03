using System;
using System.Reactive.Linq;
using Lumper.UI.ViewModels.Matchers;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp;

// BspViewModel support for searching <see cref="Lumper.Lib.BSP.BspFile"/>
public partial class BspViewModel
{
    private string _searchPattern = "";
    private MatcherBase _selectedMatcher = new GlobMatcherViewModel();

    public string SearchPattern
    {
        get => _searchPattern;
        set => this.RaiseAndSetIfChanged(ref _searchPattern, value);
    }

    public MatcherBase SelectedMatcher
    {
        get => _selectedMatcher;
        set => this.RaiseAndSetIfChanged(ref _selectedMatcher, value);
    }

    private void SearchInit()
    {
        this.WhenAnyValue(x => x.SearchPattern, x => x.SelectedMatcher
        , x => x.BspNode)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(Search);
    }

    private static async void Search(
        (string?, MatcherBase?, BspNodeBase) args)
    {
        (string? pattern, var matcherBase, var bspNode) = args;
        if (matcherBase is null || pattern is null)
            return;
        //TODO: Add lock when search is slower than throttle rate
        var matcher = matcherBase.ConstructMatcher(pattern.Trim());
        await bspNode.Filter(matcher);
    }
}
