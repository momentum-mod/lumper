namespace Lumper.UI.ViewModels.Pages.EntityEditor;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData.Binding;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Entity;
using Lumper.UI.Views.Pages.EntityEditor;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

/// <summary>
/// ViewModel for the EntityEditor page
/// </summary>
public sealed class EntityEditorViewModel : ViewModelWithView<EntityEditorViewModel, EntityEditorView>
{
    [ObservableAsProperty]
    public EntityLumpViewModel? EntityLumpViewModel { get; }

    [ObservableAsProperty]
    public ReadOnlyCollection<EntityViewModel>? FilteredEntities { get; }

    [Reactive]
    public bool IsFiltered { get; private set; }

    public EntityEditorFilters Filters { get; } = new();

    public ObservableCollection<EntityEditorTabViewModel> Tabs { get; } = [];

    [Reactive]
    public EntityEditorTabViewModel? SelectedTab { get; set; }

    public static BspService BspService => BspService.Instance;

    public EntityEditorViewModel()
    {
        var u = new Unit();

        BspService.WhenAnyValue(x => x.EntityLumpViewModel).ToPropertyEx(this, x => x.EntityLumpViewModel);

        // Switchmap entity lump changes (from BSP loads usually) into observable of changes to all entities,
        // then combine with filter changes.
        //
        // Note that we can't use a DynamicData approach of .Filter/.Transform on ChangeSet<EntityViewModel>, since
        // when filters change, we need to filter the *entity* entity lump, not just the items in the changeset
        this.WhenAnyValue(x => x.EntityLumpViewModel)
            .ObserveOn(RxApp.TaskpoolScheduler)
            // Watch changes to the entity collection. We don't care *what* entities have changed, just that
            // *something* changed since we're using class members (maybe bad practice, whatever), we don't care about
            // the value we're notifying with anywhere, just use the same Unit.
            .Select(entLump =>
                entLump is not null
                    // Note for that updating entity properties to trigger filter changes we'd need to set up an
                    // AutoRefresh() observable on *every* observable, which would be prohibitively expensive.
                    // If we really wanted this, we'd need to do some spaghetti to make property updates raise something
                    // on the parent entity, not bothering for now.
                    ? entLump.Entities.Connect().Select(_ => u)
                    // Null if entity lump was closed, just return null which we'll use to generate an empty list below.
                    : Observable.Return(u)
            )
            // Cancel previous observable from Connect() when ent lump changes.
            .Switch()
            // Also notify when any filters change. Thus we get a single observable stream that notifies whenever:
            //   (a) - The whole entity lump changes - i.e. BspService.Instance.EntityLumpViewModel is set to a new
            //         value (so, a reference to a new class)
            //   (b) - the Entities SourceCache in the ELVM is updated (additions, removals, updates)
            //   (c) - the filters change
            .CombineLatest(
                this.WhenAnyValue(x => x.Filters) // Needed to kick this off
                    .Merge(Filters.WhenAnyPropertyChanged().Throttle(TimeSpan.FromMilliseconds(100))),
                (_, _) => u
            )
            .Select(_ =>
            {
                // No ELVM loaded, just clear the list and don't bother with filters
                if (EntityLumpViewModel is null)
                {
                    IsFiltered = false;
                    Tabs.Clear();
                    SelectedTab = null;
                    return new List<EntityViewModel>().AsReadOnly();
                }

                // Run filter logic
                IsFiltered = Filter(
                    EntityLumpViewModel.Entities.Items,
                    out IEnumerable<EntityViewModel> filteredEntities
                );

                // Expand out to full list, set FilteredEntities to it
                return filteredEntities.ToList().AsReadOnly();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.FilteredEntities);
    }

    private bool Filter(IEnumerable<EntityViewModel> input, out IEnumerable<EntityViewModel> output)
    {
        bool filtered = false;
        output = input;

        bool wc = Filters.WildcardWrapping;

        if (!string.IsNullOrWhiteSpace(Filters.Classname))
        {
            filtered = true;
            output = output.Where(vm => vm.MatchClassname(Filters.Classname, wc));
        }

        bool hasKeys = !string.IsNullOrWhiteSpace(Filters.Key);
        bool hasValues = !string.IsNullOrWhiteSpace(Filters.Value);
        if (hasKeys || hasValues)
        {
            filtered = true;
            if (hasKeys && hasValues)
                output = output.Where(vm =>
                    vm.Properties.Any(p => p.MatchKey(Filters.Key, wc) && p.MatchValue(Filters.Value, wc))
                );
            else if (hasKeys)
                output = output.Where(vm => vm.Properties.Any(p => p.MatchKey(Filters.Key, wc)));
            else
                output = output.Where(vm => vm.Properties.Any(p => p.MatchValue(Filters.Value, wc)));
        }

        if (Filters.TryParseSphere(out (Vector3 position, int radius)? sphere))
        {
            filtered = true;
            output = output.Where(vm => vm.Entity.IsWithinSphere(sphere.Value.position, sphere.Value.radius));
        }

        if (!Filters.ShowBrushEntities || !Filters.ShowPointEntities)
        {
            filtered = true;

            if (!Filters.ShowBrushEntities)
                output = Filters.ShowPointEntities
                    ? output.Where(vm => !vm.Entity.IsBrushEntity)
                    : output.Where(_ => false); // Both are unchecked ¯\_(ツ)_/¯
            else
                output = output.Where(vm => vm.Entity.IsBrushEntity);
        }

        return filtered;
    }

    public void SelectTab(EntityViewModel? model)
    {
        if (model is null || model == SelectedTab?.Entity)
            return;

        if (SelectedTab is not null && !SelectedTab.Entity.IsModified && !SelectedTab.IsPinned)
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
        if (!tab.IsPinned && tab != SelectedTab)
            Tabs.Remove(tab);
    }

    public void CloseSelectedTab() => CloseTab(SelectedTab);

    public void CloseTab(EntityEditorTabViewModel? tab)
    {
        if (tab is null)
            return;

        int tabIndex = Tabs.IndexOf(tab);
        if (tabIndex == -1)
            return;

        // If we have other tabs, set new tab to tab to the left, unless
        // we're the leftmost tab, in which case use tab to the right.
        SelectedTab = Tabs.Count > 1 ? Tabs[tabIndex == 0 ? 1 : tabIndex - 1] : null;

        Tabs.RemoveAt(tabIndex);
    }
}
