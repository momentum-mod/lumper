namespace Lumper.UI.Views.Pages.EntityEditor;

using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ViewModels.Pages.EntityEditor;
using ViewModels.Shared.Entity;

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
