namespace Lumper.UI.ViewModels.Pages.Jobs;

using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lumper.Lib.Jobs;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Pakfile;
using Lumper.UI.Views.Pages.Jobs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public class RunExternalToolJobViewModel : JobViewModel
{
    public override RunExternalToolJob Job { get; }

    [Reactive]
    public string? Path { get; set; }

    [Reactive]
    public string? WorkingDir { get; set; }

    [Reactive]
    public bool WritesToStdOut { get; set; }

    [Reactive]
    public bool WritesToInputFile { get; set; }

    public RunExternalToolJobViewModel(RunExternalToolJob job)
        : base(job)
    {
        RegisterView<RunExternalToolJobViewModel, RunExternalToolJobView>();

        Job = job;
        Path = job.Path;
        WorkingDir = job.WorkingDir;
        WritesToStdOut = job.WritesToStdOut;
        WritesToInputFile = job.WritesToInputFile;

        this.WhenAnyValue(x => x.Path).BindTo(this, x => x.Job.Path);

        this.WhenAnyValue(x => x.WorkingDir).BindTo(this, x => x.Job.WorkingDir);

        this.WhenAnyValue(x => x.WritesToStdOut) // @formatter:off
            .Do(x =>
            {
                if (x)
                    WritesToInputFile = false;
            })
            .BindTo(this, x => x.Job.WritesToStdOut);

        this.WhenAnyValue(x => x.WritesToInputFile)
            .Do(x =>
            {
                if (x)
                    WritesToStdOut = false;
            })
            .BindTo(this, x => x.Job.WritesToInputFile);
    }

    protected override void Prepare() => BspService.Instance.PushChangesToModel();

    protected override void OnSuccess()
    {
        // Don't have a way to dynamically reload paklump, just reset it
        BspService.Instance.ResetLumpViewModel(typeof(PakfileLumpViewModel));
        BspService.Instance.EntityLumpViewModel?.PullChangesFromModel();
    }

    public async Task ShowFilePickerDialog()
    {
        IReadOnlyList<IStorageFile> result = await Program.MainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = "Pick Executable", AllowMultiple = false }
        );

        if (result.Count > 0)
            Path = result[0].Path.LocalPath;
    }

    public async Task ShowFolderPickerDialog()
    {
        IReadOnlyList<IStorageFolder> result = await Program.MainWindow.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Pick Working Directory" }
        );

        if (result.Count > 0)
            WorkingDir = result[0].Path.LocalPath;
    }
}
