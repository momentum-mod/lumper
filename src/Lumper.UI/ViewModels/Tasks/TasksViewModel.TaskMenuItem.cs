using System;
using Lumper.Lib.Tasks;

namespace Lumper.UI.ViewModels.Tasks;

/// <summary>
///     ViewModel for Tasks
/// </summary>
public partial class TasksViewModel : ViewModelBase
{
    public class TaskMenuItem
    {
        public TaskMenuItem(TasksViewModel TasksVM, Type t)
        {
            _tasksVM = TasksVM;
            TaskType = t;
            Name = t.Name;
        }
        private readonly TasksViewModel _tasksVM;
        public string Name { get; set; }
        private Type TaskType { get; set; }
        public void Create(object o)
        {
            object? x = Activator.CreateInstance(TaskType, new object[] { });
            if (x is LumperTask lumperTask)
            {
                var task = _tasksVM.CreateTaskViewModel(lumperTask);
                _tasksVM.Tasks.Add(task);
            }
        }
    }
}
