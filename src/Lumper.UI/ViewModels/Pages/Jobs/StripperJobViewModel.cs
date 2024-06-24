namespace Lumper.UI.ViewModels.Pages.Jobs;

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lumper.Lib.Jobs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;

public class StripperJobViewModel : JobViewModel
{
    public override StripperJob Job { get; }

    [Reactive]
    public string? ConfigPath { get; set; }

    public StripperJobViewModel(StripperJob job) : base(job)
    {
        Job = job;
        ConfigPath = job.ConfigPath;
        this.WhenAnyValue(x => x.ConfigPath).BindTo(this, x => x.Job.ConfigPath);
    }

    protected override void OnSuccess() => BspService.Instance.EntityLumpViewModel?.UpdateViewModelFromModel();

    public async Task ShowFilePickerDialog()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        IReadOnlyList<IStorageFile> result =
            await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(GenerateFilePickerOptions());

        if (result.Count == 0)
            return;

        ConfigPath = result[0].Path.LocalPath;
    }

    private static FilePickerOpenOptions GenerateFilePickerOptions() => new() {
        Title = "Pick Stripper Config",
        AllowMultiple = false,
        FileTypeFilter = [new FilePickerFileType("Stripper Config") { Patterns = new[] { "*.cfg" } }]
    };
}
