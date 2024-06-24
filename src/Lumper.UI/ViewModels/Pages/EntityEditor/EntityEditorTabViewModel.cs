namespace Lumper.UI.ViewModels.Pages.EntityEditor;

using Shared.Entity;

public class EntityEditorTabViewModel(EntityViewModel entity)
{
    public EntityViewModel Entity => entity;

    public bool IsPinned { get; set; }
}
