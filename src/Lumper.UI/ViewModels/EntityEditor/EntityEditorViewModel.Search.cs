namespace Lumper.UI.ViewModels.EntityEditor;
using System;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ViewModels;
using ViewModels.Matchers;
using Models;

public sealed partial class EntityEditorViewModel
{
    [Reactive]
    public MatcherBase SelectedMatcher { get; set; } = new GlobMatcherViewModel();

    [Reactive]
    public string SearchPattern { get; set; } = "";

    // private void SearchInit() => this.WhenAnyValue(x => x.SearchPattern, x => x.SelectedMatcher)
    //     .Throttle(TimeSpan.FromMilliseconds(400))
    //     .ObserveOn(RxApp.MainThreadScheduler)
    //     .Subscribe(Search);

    private static async void Search((string?, MatcherBase?, BspNodeBase) args)
    {
        // TODO
        // (var pattern, MatcherBase? matcherBase, BspNodeBase? bspNode) = args;
        // if (matcherBase is null || pattern is null)
        //     return;
        //
        // //TODO: Add lock when search is slower than throttle rate
        // Matcher matcher = matcherBase.ConstructMatcher(pattern.Trim());
        // await bspNode.Filter(matcher);
    }
}
