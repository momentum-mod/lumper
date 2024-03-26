namespace Lumper.UI.ViewModels.Tasks;
using Lumper.Lib.BSP;
using Lumper.Lib.Tasks;
using ReactiveUI;

public enum TaskStatus
{
    Unknown,
    Waiting,
    Running,
    Success,
    Failed

}
/// <summary>
///     ViewModel for Stripper Task
/// </summary>
public class TaskViewModel : ViewModelBase
{

    public TaskViewModel(LumperTask task)
    {
        Task = task;
        Task.Progress.OnPercentChanged +=
            (object source, double newPercent) => this.RaisePropertyChanged(nameof(ProgressPercent));
    }

    public LumperTask Task
    {
        get;
    }
    public double ProgressPercent => Task.Progress.Percent;
    public TaskStatus _status;
    public TaskStatus Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }

    public bool Run(BspFile bsp)
    {
        var success = true;
        Status = TaskStatus.Running;
        switch (Task.Run(bsp))
        {
            case TaskResult.Success:
                Status = TaskStatus.Success;
                break;
            case TaskResult.Failed:
                Status = TaskStatus.Failed;
                success = false;
                break;
            case TaskResult.Unknown:
                Status = TaskStatus.Unknown;
                break;
            default:
                break;
        }
        return success;
    }

    public void Reset()
    {
        Status = TaskStatus.Waiting;
        Task.Progress.Reset();
        IsRunning = false;
    }

}
