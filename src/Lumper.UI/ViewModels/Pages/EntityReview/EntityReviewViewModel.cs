namespace Lumper.UI.ViewModels.Pages.EntityReview;

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lib.Util;
using Lumper.UI.Views.Pages.EntityReview;
using ReactiveUI.Fody.Helpers;
using Services;

public sealed class EntityReviewViewModel : ViewModelWithView<EntityReviewViewModel, EntityReviewView>
{
    [Reactive]
    public string? RulesFilePath { get; set; } = EntityRule.DefaultRulesPath;

    // TODO next:
    // - Get filteration started straight away, using same techniques as the entity editor
    //   - That's gonna justify us using dynamicdata transform/filter stuff
    public EntityReviewViewModel() => _ = EntityRuleService.Instance.LoadRules(RulesFilePath);

    public async Task ShowFilePickerDialog()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        IReadOnlyList<IStorageFile> result = await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(
            GenerateFilePickerOptions()
        );

        if (result.Count == 0)
            return;

        RulesFilePath = result[0].Path.LocalPath;

        _ = EntityRuleService.Instance.LoadRules(RulesFilePath);
    }

    private static FilePickerOpenOptions GenerateFilePickerOptions() =>
        new()
        {
            Title = "Pick Entity Rule File",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Entity Rule File") { Patterns = ["*.json"] }],
        };
}
