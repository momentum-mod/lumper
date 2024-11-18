namespace Lumper.UI.Views.Pages.EntityEditor;

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
        var selected = (EntityViewModel?)EntityList?.SelectedItem;

        if (selected is not null)
            ViewModel?.SelectTab(selected);
    }
}
