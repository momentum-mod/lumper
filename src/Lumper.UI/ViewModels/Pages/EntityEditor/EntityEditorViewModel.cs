namespace Lumper.UI.ViewModels.Pages.EntityEditor;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Matchers;
using Models.Matchers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;
using Shared.Entity;
using Views.Pages.EntityEditor;

/// <summary>
/// ViewModel for the EntityEditor page
/// </summary>
public sealed class EntityEditorViewModel : ViewModelWithView<EntityEditorViewModel, EntityEditorView>
{
    [ObservableAsProperty]
    public EntityLumpViewModel? EntityLumpViewModel { get; }

    [ObservableAsProperty]
    public ReadOnlyCollection<EntityViewModel>? FilteredEntities { get; }

    [ObservableAsProperty]
    public int FilteredCount { get; }

    [Reactive]
    public bool IsFiltered { get; private set; }

    [Reactive]
    private MatcherViewModel? SelectedMatcher { get; set; } = new SimpleMatcherViewModel();

    [Reactive]
    public string SearchPattern { get; set; } = "";

    [ObservableAsProperty]
    public MatcherViewModel? Matcher { get; }

    public ObservableCollection<EntityEditorTabViewModel> Tabs { get; } = [];

    [Reactive]
    public EntityEditorTabViewModel? SelectedTab { get; set; }

    public static BspService BspService => BspService.Instance;

    public EntityEditorViewModel()
    {
        // Track changes to the matchers, the entity lump being replaced, or any changes to the current entity lump,
        // then generate a fresh list of entities.
        //
        // Note that because different matchers result in a completely new FilteredEntities
        // collection, there's no point binding an IChangeSet to a ReadonlyObservableCollection.
        // We could do that for entity additions/deletions, but then code is more complicated
        // and those cases are rarer than searches. So every time the matcher, entity lump, or *contents*
        // of the entity lump change, run the filter and output to FilteredEntities, which is just
        // a reactive IEnumerable.
        this.WhenAnyValue(x => x.SearchPattern)
            .ObserveOn(RxApp.TaskpoolScheduler)
            // Throttle changes to search pattern
            .Throttle(TimeSpan.FromMilliseconds(100))
            .CombineLatest(this.WhenAnyValue(x => x.SelectedMatcher))
            // Combine latest of each seach pattern and selected matcher type into a Matcher
            .Select(tuple =>
            {
                (var pattern, MatcherViewModel? matcher) = tuple;
                return matcher is not null && !string.IsNullOrWhiteSpace(pattern)
                    ? matcher.ConstructMatcher(pattern)
                    : null;
            })
            // Combine latest with overall lump changes, e.g. new BSP is loaded
            .CombineLatest(BspService.Instance.WhenAnyValue(x => x.EntityLumpViewModel))
            // Also watch changes to the entity collection. We don't care *what* entities have changed, just that
            // *something* changed, and we still want the incoming values (matchers and ent lump), so map those values
            // back out.
            // Thus we get a single observable stream that notifies whenever:
            //   (a) - the matchers changed
            //   (b) - the whole entity lump changes - i.e. BspService.Instance.EntityLumpViewModel is set to a new
            //         value (so, a reference to a new class)
            //   (c) - the Entities SourceCache in the ELVM is updated (additions, removals, updates)
            //         where the *values* it notifies with are just the current matcher and the ELVM.
            .Select(tuple =>
                tuple.Second is not null
                    ? tuple.Second.Entities.Connect().Select(_ => tuple)
                    // Null if entity lump was closed, just return tuple so can generate empty list
                    : Observable.Return(tuple)
            )
            .Switch()
            .Select(tuple =>
            {
                (Matcher? matcher, EntityLumpViewModel? entLump) = tuple;

                // No ELVM loaded, probably no BSP loaded: empty list.
                if (entLump is null)
                {
                    IsFiltered = false;
                    Tabs.Clear();
                    SelectedTab = null;
                    return new List<EntityViewModel>().AsReadOnly();
                }

                // No matchers: readonly list of all the entities in the ELVM.
                if (matcher is null)
                {
                    IsFiltered = false;
                    return entLump.Entities.Items.ToList().AsReadOnly();
                }

                // We have a matcher, filter the entity list. Note that the above ObserveOn ensures we're on a
                // separate thread if available, as filteration searches every *property* of every entity, so
                // quite expensive.
                IsFiltered = true;
                return entLump.Entities.Items.Where(entity => entity.Match(matcher)).ToList().AsReadOnly();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.FilteredEntities);

        this.WhenAnyValue(x => x.FilteredEntities).Select(x => x?.Count ?? 0).ToPropertyEx(this, x => x.FilteredCount);

        BspService.WhenAnyValue(x => x.EntityLumpViewModel).ToPropertyEx(this, x => x.EntityLumpViewModel);
    }

    public void SelectTab(EntityViewModel? model)
    {
        if (model is null || model == SelectedTab?.Entity)
            return;

        if (SelectedTab is not null && SelectedTab.Entity.IsModified == false && SelectedTab.IsPinned == false)
            Tabs.Remove(SelectedTab);

        EntityEditorTabViewModel? newTab = Tabs.FirstOrDefault(x => x.Entity == model);
        if (newTab is null)
        {
            newTab = new EntityEditorTabViewModel(model);
            Tabs.Add(newTab);
        }

        SelectedTab = newTab;
    }

    public void TogglePinnedTab(EntityEditorTabViewModel tab)
    {
        if (tab.IsPinned == false && tab != SelectedTab)
            Tabs.Remove(tab);
    }

    public void CloseSelectedTab() => CloseTab(SelectedTab);

    public void CloseTab(EntityEditorTabViewModel? tab)
    {
        if (tab is null)
            return;

        var tabIndex = Tabs.IndexOf(tab);
        if (tabIndex == -1)
            return;

        // If we have other tabs, set new tab to tab to the left, unless
        // we're the leftmost tab, in which case use tab to the right.
        SelectedTab = Tabs.Count > 1 ? Tabs[tabIndex == 0 ? 1 : tabIndex - 1] : null;

        Tabs.RemoveAt(tabIndex);
    }
}
