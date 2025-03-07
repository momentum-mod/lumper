namespace Lumper.UI.ViewModels.Pages.EntityEditor;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData.Binding;
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

    public static GameSyncService GameSyncService => GameSyncService.Instance;

    public EntityEditorViewModel()
    {
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
                    ? entLump.Entities.Connect().Select(_ => Unit.Default)
                    // Null if entity lump was closed, just return null which we'll use to generate an empty list below.
                    : Observable.Return(Unit.Default)
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
                    .Merge(Filters.WhenAnyPropertyChanged().Throttle(TimeSpan.FromMilliseconds(10))),
                (_, _) => Unit.Default
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

        Filters
            .WhenAnyValue(x => x.SyncPlayerPosition)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(enabled =>
            {
                if (!enabled)
                    return Observable.Empty<string>();

                Filters.SyncTargetEntity = false;

                Filters.Key = string.Empty;
                Filters.Value = string.Empty;

                return GameSyncService.WhenAnyValue(x => x.PlayerPosition);
            })
            .Switch()
            .Select(pos =>
                // Remove decimal points, clogs up filter textbox and will never need such high precision.
                pos is not null
                    ? string.Join(" ", pos.Split(' ').Select(str => float.TryParse(str, out float val) ? (int)val : 0))
                    : null
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(pos =>
            {
                if (pos is not null)
                    Filters.SpherePosition = pos;
            });

        Filters
            .WhenAnyValue(x => x.SyncTargetEntity)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(enabled =>
            {
                if (!enabled)
                    return Observable.Empty<string>();

                Filters.SyncPlayerPosition = false;
                Filters.SpherePosition = string.Empty;

                return GameSyncService.WhenAnyValue(x => x.TargetEntities);
            })
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ents =>
            {
                if (!string.IsNullOrWhiteSpace(ents))
                {
                    Filters.Key = "model";
                    Filters.Value = ents.Replace("*", "\\*").Replace(',', '|');
                }
                else
                {
                    Filters.Key = string.Empty;
                    Filters.Value = string.Empty;
                }
            });
    }

    private bool Filter(IEnumerable<EntityViewModel> input, out IEnumerable<EntityViewModel> output)
    {
        bool filtered = false;
        output = input;

        if (
            EntityEditorFilters.TryParseStringFilters(
                Filters.Classname,
                out List<string>? inclClassname,
                out List<string>? exclClassname
            )
        )
        {
            filtered = true;
            output = output.Where(vm =>
                (inclClassname.Count == 0 || inclClassname.Any(classname => vm.MatchClassname(classname, true)))
                && !exclClassname.Any(classname => vm.MatchClassname(classname, true))
            );
        }

        bool hasKeys = EntityEditorFilters.TryParseStringFilters(
            Filters.Key,
            out List<string>? inKeys,
            out List<string>? exKeys
        );
        bool hasVals = EntityEditorFilters.TryParseStringFilters(
            Filters.Value,
            out List<string>? inVals,
            out List<string>? exVals
        );

        // Using the wildcard wrapping behaviour for everything here (e.g. BCD matches ABCDE),
        // usage is very unintuitive without wrapping enabled and really don't want to have
        // a separate checkbox for it.
        if (hasKeys || hasVals)
        {
            filtered = true;
            if (hasKeys && hasVals)
            {
                output = output.Where(vm =>
                    vm.Properties.Any(prop =>
                        (inKeys!.Count == 0 || inKeys.Any(key => prop.MatchKey(key, true)))
                        && !exKeys!.Any(key => prop.MatchKey(key, true))
                        && (inVals!.Count == 0 || inVals.Any(val => prop.MatchValue(val, true)))
                        && !exVals!.Any(val => prop.MatchValue(val, true))
                    )
                );
            }
            else if (hasKeys)
            {
                output = output.Where(vm =>
                    vm.Properties.Any(prop =>
                        (inKeys!.Count == 0 || inKeys.Any(key => prop.MatchKey(key, true)))
                        && !exKeys!.Any(key => prop.MatchKey(key, true))
                    )
                );
            }
            else
            {
                output = output.Where(vm =>
                    vm.Properties.Any(prop =>
                        (inVals!.Count == 0 || inVals.Any(val => prop.MatchValue(val, true)))
                        && !exVals!.Any(val => prop.MatchValue(val, true))
                    )
                );
            }
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

        if (Filters.TryParseSphere(out (Vector3 position, int radius)? sphere))
        {
            filtered = true;
            output = output.Where(vm => vm.Entity.IsWithinSphere(sphere.Value.position, sphere.Value.radius));
        }

        return filtered;
    }

    public void DeleteSelected()
    {
        if (SelectedTab is EntityEditorTabSingleEntityViewModel single)
            EntityLumpViewModel?.RemoveEntity(single.Entity);
        else if (SelectedTab is EntityEditorTabMultipleEntityViewModel multiple)
            EntityLumpViewModel?.RemoveMultiple(multiple.RealEntities);
    }

    public void OpenTab(List<EntityViewModel>? entities)
    {
        if (entities is null or { Count: 0 })
            return;

        if (SelectedTab is EntityEditorTabSingleEntityViewModel { IsPinned: false, Entity.IsModified: false })
        {
            Tabs.Remove(SelectedTab);
            SelectedTab = null;
        }

        // Never preserve tabs for multiple entity selections, too weird, confusing UX, don't like multiple tabs
        // with textentries bound to same entity.
        if (SelectedTab is EntityEditorTabMultipleEntityViewModel multiTab)
        {
            Tabs.Remove(SelectedTab);
            SelectedTab = null;
            multiTab.Dispose();
        }

        if (entities is [{ } singleEntity])
        {
            EntityEditorTabSingleEntityViewModel? newTab = Tabs.OfType<EntityEditorTabSingleEntityViewModel>()
                .FirstOrDefault(tab => tab.Entity == singleEntity);

            if (newTab is null)
            {
                newTab = new EntityEditorTabSingleEntityViewModel(singleEntity);
                Tabs.Add(newTab);
            }

            SelectedTab = newTab;
        }
        else
        {
            var newTab = new EntityEditorTabMultipleEntityViewModel(entities);
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }
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
