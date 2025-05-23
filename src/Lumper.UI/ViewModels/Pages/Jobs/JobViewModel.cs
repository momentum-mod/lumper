namespace Lumper.UI.ViewModels.Pages.Jobs;

using Lumper.Lib.Bsp;
using Lumper.Lib.Jobs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public enum JobStatus
{
    Success,
    Waiting,
    Running,
    Failed,
}

public abstract class JobViewModel : ViewModel
{
    protected JobViewModel(Job job)
    {
        Job = job;
        job.Progress.OnPercentChanged += (_, _) => this.RaisePropertyChanged(nameof(ProgressPercent));
    }

    public virtual Job Job { get; }

    public double ProgressPercent => Job.Progress.Percent;

    [Reactive]
    public JobStatus Status { get; set; } = JobStatus.Waiting;

    [Reactive]
    public bool IsRunning { get; set; }

    public bool Run(BspFile bsp)
    {
        Status = JobStatus.Running;

        bool status = Job.Run(bsp);

        if (status)
        {
            Status = JobStatus.Success;
            return true;
        }
        else
        {
            Status = JobStatus.Failed;
            return false;
        }
    }

    public void Reset()
    {
        Status = JobStatus.Waiting;
        Job.Progress.Reset();
        IsRunning = false;
    }
}
