using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Newtonsoft.Json;
using Lumper.Lib.Tasks;
using Lumper.Lib.BSP;

namespace Lumper.UI.ViewModels.Tasks;

/// <summary>
///     ViewModel for Tasks
/// </summary>
public partial class TasksViewModel : ViewModelBase
{
    public TasksViewModel(BspFile bsp)
    {
        BspFile = bsp;
        TaskTypes = new()
        {
            new TaskMenuItem(this, typeof(ChangeTextureTask)),
            new TaskMenuItem(this, typeof(StripperTask)),
            new TaskMenuItem(this, typeof(CompressionTask)),
            new TaskMenuItem(this, typeof(RunExternalToolTask)),
        };
        this.WhenAnyPropertyChanged(nameof(SelectedTask))
         .Subscribe(_ => OnSelectedTaskChanged());
    }

    //todo load new map without creating a new TasksViewModel?
    public BspFile BspFile
    {
        get;
    }
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => this.RaiseAndSetIfChanged(ref _isRunning, value);
    }
    private TaskViewModel? _selectedTask;
    public TaskViewModel? SelectedTask
    {
        get => _selectedTask;
        set => this.RaiseAndSetIfChanged(ref _selectedTask, value);
    }
    public List<TaskMenuItem> TaskTypes { get; private set; }
    public ObservableCollection<TaskViewModel> Tasks { get; private set; } = new();

    private ViewModelBase? _content;
    public ViewModelBase? Content
    {
        get => _content;
        private set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    private void OnSelectedTaskChanged()
    {
        Content = SelectedTask ?? null;
    }

    public TaskViewModel CreateTaskViewModel(LumperTask lumperTask)
    {
        return lumperTask switch
        {
            StripperTask stripperTask => new StripperTaskViewModel(stripperTask),
            RunExternalToolTask runExternal => new RunExternalToolTaskViewModel(runExternal),
            ChangeTextureTask changeTexture => new ChangeTextureTaskViewModel(changeTexture),
            CompressionTask compression => new CompressionTaskViewModel(compression),
            _ => new TaskViewModel(lumperTask)
        };
    }

    private enum MoveDir { Up = -1, Down = 1 }
    public void MoveSelectedTaskUp()
    {
        MoveSelectedTaskDir(MoveDir.Up);
    }
    public void MoveSelectedTaskDown()
    {
        MoveSelectedTaskDir(MoveDir.Down);
    }

    private void MoveSelectedTaskDir(MoveDir dir)
    {
        if (SelectedTask is null || Tasks.Count <= 1)
            return;

        int offset = (int)dir;
        int idx = Tasks.IndexOf(SelectedTask);
        int newIdx = idx + offset;

        if (newIdx < 0)
            newIdx = Tasks.Count + offset;
        else if (newIdx >= Tasks.Count)
            newIdx = 0;

        Tasks.Move(idx, newIdx);
        SelectedTask = null;
        SelectedTask = Tasks[newIdx];
    }

    public void RemoveSelectedTask()
    {
        if (SelectedTask is null || Tasks.Count < 1)
            return;
        Tasks.Remove(SelectedTask);
        Content = null;
    }

    public async Task Run()
    {
        IsRunning = true;
        await Task.Run(() =>
        {
            try
            {
                foreach (var task in Tasks)
                {
                    task.Reset();
                }
                foreach (var task in Tasks)
                {
                    if (!task.Run(BspFile))
                        return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            IsRunning = false;
        });
    }

    public void Load(Stream stream)
    {
        var serializer = new JsonSerializer()
        {
            Formatting = Formatting.Indented
        };
        using var sr = new StreamReader(stream);
        using var reader = new JsonTextReader(sr);
        var tasks = serializer.Deserialize<List<LumperTask>>(reader);
        if (tasks == null)
            return;
        Tasks.Clear();
        SelectedTask = null;
        foreach (var task in tasks)
        {
            Tasks.Add(CreateTaskViewModel(task));
        }
    }

    public void Save(Stream stream)
    {
        var serializer = new JsonSerializer()
        {
            Formatting = Formatting.Indented
        };
        using var sw = new StreamWriter(stream);
        using var writer = new JsonTextWriter(sw);
        serializer.Serialize(writer, Tasks.Select(x => x.Task));
    }
}
