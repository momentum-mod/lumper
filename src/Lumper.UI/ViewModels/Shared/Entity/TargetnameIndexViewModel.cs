namespace Lumper.UI.ViewModels.Shared.Entity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Lumper.UI.Controls;
using Lumper.UI.Services;
using ReactiveUI;

public sealed record TargetnameMapping(string Targetname, string Classname);

public sealed class TargetnameIndexViewModel : ViewModel, IDisposable
{
    public ObservableCollectionExtended<TargetnameMapping> Entries { get; } = [];
    public ObservableCollectionExtended<ExtendedAutoCompleteItem> Suggestions { get; } = [];
    public IReadOnlyDictionary<string, string> ClassnamesByTargetname { get; private set; } =
        new Dictionary<string, string>();

    private readonly IDisposable _subscription;

    public static BspService BspService => BspService.Instance;

    public TargetnameIndexViewModel()
    {
        _subscription = BspService
            .WhenAnyValue(x => x.EntityLumpViewModelLazy)
            .Select(entLump =>
                entLump is not null
                    ? entLump
                        .Entities.Connect()
                        .AutoRefresh(e => e.Targetname)
                        .AutoRefresh(e => e.Classname)
                        .ToCollection()
                    : Observable.Return<IReadOnlyCollection<EntityViewModel>>([])
            )
            .Switch()
            .Publish(shared =>
                Observable.Merge(
                    shared.Take(1),
                    shared.Skip(1).Throttle(TimeSpan.FromMilliseconds(200), RxApp.TaskpoolScheduler)
                )
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(entities =>
                entities
                    .Where(e => !string.IsNullOrEmpty(e.Targetname))
                    .GroupBy(e => e.Targetname!)
                    .Select(g => new TargetnameMapping(g.Key, g.First().Classname))
                    .ToList()
            )
            .Subscribe(list =>
            {
                Entries.Clear();
                Entries.AddRange(list);

                Suggestions.Clear();
                Suggestions.AddRange(
                    list.Select(entry => new ExtendedAutoCompleteItem
                    {
                        Value = entry.Targetname,
                        Display = $"{entry.Targetname} ({entry.Classname})",
                    })
                );

                ClassnamesByTargetname = list.ToDictionary(entry => entry.Targetname, entry => entry.Classname);
            });
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
