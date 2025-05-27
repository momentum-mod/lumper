namespace Lumper.UI.ViewModels.Pages.RequiredGames;

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lumper.Lib.RequiredGames;
using Lumper.UI.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Views.Pages.RequiredGames;

public sealed class RequiredGamesViewModel : ViewModelWithView<RequiredGamesViewModel, RequiredGamesView>, IDisposable
{
    [Reactive]
    public List<RequiredGames.PackedAsset> Results { get; set; } = [];

    [Reactive]
    public string Required { get; set; } = "";

    private readonly Subject<Unit> _recomputer = new();

    public RequiredGamesViewModel()
    {
        BspService
            .Instance.WhenAnyValue(x => x.BspFile)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .CombineLatest(_recomputer)
            .Select(tup => RequiredGames.GetRequiredGames(tup.First))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(t =>
            {
                Results = t.assets;
                Required = t.summary;
            });

        Recompute();
    }

    private void Recompute()
    {
        _recomputer.OnNext(Unit.Default);
    }

    public void Dispose()
    {
        _recomputer.Dispose();
    }
}
