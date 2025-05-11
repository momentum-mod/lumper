namespace Lumper.UI.ViewModels.Pages.Jobs;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lumper.Lib.Bsp;
using Lumper.Lib.Jobs;
using Lumper.UI.Services;
using Lumper.UI.Views.Pages.Jobs;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public class JobsViewModel : ViewModelWithView<JobsViewModel, JobsView>
{
    public JobsViewModel()
    {
        JobTypes =
        [
            new JobMenuItem<RemoveAssetJob>(this, () => new RemoveAssetJob()),
            new JobMenuItem<ReplaceTextureJob>(this, () => new ReplaceTextureJob()),
            new JobMenuItem<StripperFileJob>(this, () => new StripperFileJob()),
            new JobMenuItem<StripperTextJob>(this, () => new StripperTextJob()),
            new JobMenuItem<RunExternalToolJob>(this, () => new RunExternalToolJob()),
        ];

        this.WhenAnyValue(x => x.SelectedJob).Where(x => x is not null).BindTo(this, x => x.ActiveJobPage);

        this.WhenAnyValue(
                x => x.SelectedJob,
                x => x.IsRunning,
                (selectedJob, isRunning) => selectedJob is not null && !isRunning
            )
            .ToPropertyEx(this, x => x.IsNotRunningAndHasSelection);

        BspService
            .Instance.WhenAnyValue(x => x.BspFile)
            .Subscribe(_ =>
            {
                foreach (JobViewModel job in Jobs)
                    job.Reset();
            });
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

    public static JobViewModel CreateJobViewModel(IJob job) =>
        job switch
        {
            StripperFileJob stripperFile => new StripperFileJobViewModel(stripperFile),
            StripperTextJob stripperText => new StripperTextJobViewModel(stripperText),
            RunExternalToolJob runExternal => new RunExternalToolJobViewModel(runExternal),
            ReplaceTextureJob changeTexture => new ReplaceTextureJobViewModel(changeTexture),
            RemoveAssetJob removeAsset => new RemoveAssetJobViewModel(removeAsset),
            _ => throw new ArgumentException("Invalid job"),
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
            IsRunning = false;
            return;
        }

        await Observable.Start(
            () =>
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
                            $"Job execution failed for job \"{job.Job.JobNameInternal}\"! Threw {exception.GetType()}: {exception.Message}"
                        );
                    }
                }

                IsRunning = false;

                Logger.Info("Job execution complete");
            },
            RxApp.TaskpoolScheduler
        );
    }

    private enum MoveDir
    {
        Up = -1,
        Down = 1,
    }

    public void MoveSelectedUp() => MoveSelectedInDirection(MoveDir.Up);

    public void MoveSelectedDown() => MoveSelectedInDirection(MoveDir.Down);

    private void MoveSelectedInDirection(MoveDir dir)
    {
        if (SelectedJob is null || Jobs.Count <= 1)
            return;

        int offset = (int)dir;
        int idx = Jobs.IndexOf(SelectedJob);
        int newIdx = idx + offset;

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

    public void RemoveAll()
    {
        Jobs.Clear();
        ActiveJobPage = null;
    }

    public void LoadWorkflow(Stream stream)
    {
        if (!Job.TryLoadWorkflow(stream, out List<Job>? workflow))
        {
            Logger.Warn("Could not load workflow");
            return;
        }

        SelectedJob = null;
        Jobs.Clear();
        foreach (Job job in workflow)
            Jobs.Add(CreateJobViewModel(job));

        Logger.Info("Workflow loaded");
    }

    public void SaveWorkflow(Stream stream)
    {
        Job.SaveWorkflow(stream, Jobs.Select(x => x.Job).ToList());

        Logger.Info("Workflow saved");
    }

    public async Task ShowLoadJobsFileDialog()
    {
        IReadOnlyList<IStorageFile> result = await Program.MainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Pick Jobs File",
                FileTypeFilter = GenerateJsonFileFilter(),
            }
        );

        if (result is not { Count: 1 })
            return;

        LoadWorkflow(await result[0].OpenReadAsync());
    }

    public async Task ShowSaveJobsFileDialog()
    {
        IStorageFile? result = await Program.MainWindow.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions { Title = "Save Jobs File", FileTypeChoices = GenerateJsonFileFilter() }
        );

        if (result is null)
            return;

        SaveWorkflow(await result.OpenWriteAsync());
    }

    private static FilePickerFileType[] GenerateJsonFileFilter() =>
        [
            new FilePickerFileType("JSON Files") { Patterns = ["*.json"] },
            new FilePickerFileType("All Files") { Patterns = ["*"] },
        ];
}
