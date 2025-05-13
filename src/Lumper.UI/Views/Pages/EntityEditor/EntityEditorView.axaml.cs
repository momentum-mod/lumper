namespace Lumper.UI.Views.Pages.EntityEditor;

using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Lumper.UI.ViewModels.Pages.EntityEditor;
using Lumper.UI.ViewModels.Shared.Entity;

public partial class EntityEditorView : ReactiveUserControl<EntityEditorViewModel>
{
    public EntityEditorView() => InitializeComponent();

    private void SelectAll(object? sender, RoutedEventArgs e) => EntityList?.SelectAll();

    private void EntityList_OnSelectionChanged(object? _, SelectionChangedEventArgs __) => OpenSelectedListItem();

    private void OpenSelectedListItem()
    {
        // Avalonia seems to still list SelectedItems be non-null even after the items collection is cleared,
        // we have an empty list if so, don't try to open a tab otherwise we can get a zombie tab from a closed BSP.
        if (EntityList.Items.Count == 0)
            return;

        var selected = EntityList?.SelectedItems?.OfType<EntityViewModel>().ToList();

        if (selected is not null)
            ViewModel?.OpenTab(selected);
    }
}
