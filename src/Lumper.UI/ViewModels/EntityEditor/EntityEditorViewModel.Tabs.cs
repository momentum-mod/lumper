namespace Lumper.UI.ViewModels.EntityEditor;
using System;
using System.Collections.ObjectModel;
using ReactiveUI.Fody.Helpers;

public sealed partial class EntityEditorViewModel
{
    private ObservableCollection<EntityViewModel> Tabs { get; } = [];

    [Reactive]
    public EntityViewModel? SelectedTab { get; set; }

    public void OpenTab(EntityViewModel? node)
    {
        if (node is null)
            return;

        if (!Tabs.Contains(node))
            Tabs.Add(node);

        SelectedTab = node;
    }

    public void CloseSelectedTab() => CloseTab(SelectedTab);

    public void CloseTab(EntityViewModel? bspNode)
    {
        if (bspNode is null)
            return;

        var tabIndex = Tabs.IndexOf(bspNode);
        if (tabIndex == -1)
            return;

        // If we have other tabs, set new tab to tab to the left, unless
        // we're the leftmost tab, in which case use tab to the right.
        if (Tabs.Count > 1)
            SelectedTab = Tabs[tabIndex == 0 ? 1 : tabIndex - 1];
        else
            SelectedTab = null;

        Tabs.RemoveAt(tabIndex);
    }
}
