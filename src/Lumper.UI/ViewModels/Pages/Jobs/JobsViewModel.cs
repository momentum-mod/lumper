namespace Lumper.UI.ViewModels.Pages.Jobs;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lib.BSP;
using Lumper.Lib.Jobs;
using Newtonsoft.Json;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;
using Views.Pages.Jobs;

public class JobsViewModel : ViewModelWithView<JobsViewModel, JobsView>
{
    public JobsViewModel()
    {
        JobTypes = [
            new JobMenuItem<ReplaceTextureJob>(this, () => new ReplaceTextureJob()),
            new JobMenuItem<StripperJob>(this, () => new StripperJob()),
            new JobMenuItem<RunExternalToolJob>(this, () => new RunExternalToolJob())
        ];

        this.WhenAnyValue(x => x.SelectedJob)
            .Where(x => x is not null)
            .BindTo(this, x => x.ActiveJobPage);

        this.WhenAnyValue(
                x => x.SelectedJob,
                x => x.IsRunning,
                (selectedJob, isRunning) => selectedJob is not null && !isRunning)
            .ToPropertyEx(this, x => x.IsNotRunningAndHasSelection);
    }

    [Reactive]
    public bool IsRunning { get; set; }

    [Reactive]
    public JobViewModel? SelectedJob { get; set; }

    [ObservableAsProperty]
    public bool IsNotRunningAndHasSelection { get; set; }

    public ObservableCollection<JobViewModel> Jobs { get; } = [];

    [Reactive]
    public ViewModel? ActiveJobPage { get; set; }

    public List<IJobMenuItem<Job>> JobTypes { get; init; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static JobViewModel CreateJobViewModel(IJob job) => job switch {
        StripperJob stripper => new StripperJobViewModel(stripper),
        RunExternalToolJob runExternal => new RunExternalToolJobViewModel(runExternal),
        ReplaceTextureJob changeTexture => new ReplaceTextureJobViewModel(changeTexture),
        _ => throw new ArgumentException("Invalid job")
    };

    public async Task Run()
    {
        IsRunning = true;
        BspFile? bspFile = BspService.Instance.BspFile;
        if (bspFile is null)
            return;

        if (Jobs.Count == 0)
        {
            Logger.Info("No jobs, exiting");
            return;
        }

        await Observable.Start(() =>
        {
            Logger.Info("Running job queue");

            foreach (JobViewModel job in Jobs)
            {
                job.Reset();

                try
                {
                    job.Status = job.Run(bspFile) ? JobStatus.Success : JobStatus.Failed;
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"Job execution failed for job \"{job.Job.JobNameInternal}\"! Threw {exception.GetType()}: {exception.Message}");
                }
            }

            IsRunning = false;

            Logger.Info("Job execution complete");
        }, RxApp.TaskpoolScheduler);
    }

    private enum MoveDir { Up = -1, Down = 1 }

    public void MoveSelectedUp() => MoveSelectedInDirection(MoveDir.Up);
    public void MoveSelectedDown() => MoveSelectedInDirection(MoveDir.Down);

    private void MoveSelectedInDirection(MoveDir dir)
    {
        if (SelectedJob is null || Jobs.Count <= 1)
            return;

        var offset = (int)dir;
        var idx = Jobs.IndexOf(SelectedJob);
        var newIdx = idx + offset;

        if (newIdx < 0)
            newIdx = Jobs.Count + offset;
        else if (newIdx >= Jobs.Count)
            newIdx = 0;

        Jobs.Move(idx, newIdx);
        SelectedJob = null;
        SelectedJob = Jobs[newIdx];
    }


    public void RemoveSelectedJob()
    {
        if (SelectedJob is null || Jobs.Count < 1)
            return;

        Jobs.Remove(SelectedJob);
        ActiveJobPage = null;
    }

    public void RemoveAll() => Jobs.Clear();


    public void Load(Stream stream)
    {
        var serializer = new JsonSerializer();
        using var sr = new StreamReader(stream);
        using var reader = new JsonTextReader(sr);
        List<Job>? jobs;
        try
        {
            jobs = serializer.Deserialize<List<Job>>(reader);
        }
        catch (JsonSerializationException ex)
        {
            Logger.Error(ex, "Failed to load jobs workflow");
            return;
        }

        if (jobs is null)
            return;

        SelectedJob = null;
        Jobs.Clear();
        foreach (Job job in jobs)
            Jobs.Add(CreateJobViewModel(job));
    }

    public void Save(Stream stream)
    {
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        using var sw = new StreamWriter(stream);
        using var writer = new JsonTextWriter(sw);
        serializer.Serialize(writer, Jobs.Select(x => x.Job));
    }

    public async Task ShowLoadJobsFileDialog()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        IReadOnlyList<IStorageFile> result = await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions {
                AllowMultiple = false, Title = "Pick Jobs File", FileTypeFilter = GenerateJsonFileFilter()
            });

        if (result is not { Count: 1 })
            return;

        Load(await result[0].OpenReadAsync());
    }

    public async Task ShowSaveJobsFileDialog()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        IStorageFile? result = await Program.Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions { Title = "Save Jobs File", FileTypeChoices = GenerateJsonFileFilter() });

        if (result is null)
            return;

        Save(await result.OpenWriteAsync());
    }

    private static FilePickerFileType[] GenerateJsonFileFilter() => [
        new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
    ];
}
