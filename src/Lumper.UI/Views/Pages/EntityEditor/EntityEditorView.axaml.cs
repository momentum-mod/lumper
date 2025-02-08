namespace Lumper.UI.Views.Pages.EntityEditor;

using System.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Lumper.UI.ViewModels.Pages.EntityEditor;
using Lumper.UI.ViewModels.Shared.Entity;

public partial class EntityEditorView : ReactiveUserControl<EntityEditorViewModel>
{
    public EntityEditorView() => InitializeComponent();

    private void EntityList_OnSelectionChanged(object? _, SelectionChangedEventArgs __) => OpenSelectedListItem();

    private void OpenSelectedListItem()
    {
        var selected = EntityList?.SelectedItems?.OfType<EntityViewModel>().ToList();

        if (selected is not null)
            ViewModel?.OpenTab(selected);
    }
}
