namespace Lumper.UI.ViewModels.Pages.Jobs;

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lumper.Lib.Jobs;
using Lumper.UI.Services;
using Lumper.UI.Views.Pages.Jobs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public class StripperJobViewModel : JobViewModel
{
    public override StripperJob Job { get; }

    [Reactive]
    public string? ConfigPath { get; set; }

    public StripperJobViewModel(StripperJob job)
        : base(job)
    {
        RegisterView<StripperJobViewModel, StripperJobView>();

        Job = job;
        ConfigPath = job.ConfigPath;
        this.WhenAnyValue(x => x.ConfigPath).BindTo(this, x => x.Job.ConfigPath);
    }

    protected override void OnSuccess() => BspService.Instance.EntityLumpViewModel?.UpdateViewModelFromModel();

    public async Task ShowFilePickerDialog()
    {
        IReadOnlyList<IStorageFile> result = await Program.MainWindow.StorageProvider.OpenFilePickerAsync(
            GenerateFilePickerOptions()
        );

        if (result.Count == 0)
            return;

        ConfigPath = result[0].Path.LocalPath;
    }

    private static FilePickerOpenOptions GenerateFilePickerOptions() =>
        new()
        {
            Title = "Pick Stripper Config",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Stripper Config") { Patterns = ["*.cfg"] }],
        };
}
