namespace Lumper.UI.ViewModels.Tasks;
using System;
using Lumper.Lib.Tasks;

/// <summary>
///     ViewModel for Tasks
/// </summary>
public partial class TasksViewModel : ViewModelBase
{
    public class TaskMenuItem(TasksViewModel TasksVM, Type t)
    {
        private readonly TasksViewModel _tasksVM = TasksVM;
        public string Name { get; set; } = t.Name;
        private Type TaskType { get; set; } = t;

        private static readonly object[] args = [];

        public void Create(object o)
        {
            var x = Activator.CreateInstance(TaskType, args);
            if (x is LumperTask lumperTask)
            {
                TaskViewModel task = CreateTaskViewModel(lumperTask);
                _tasksVM.Tasks.Add(task);
            }
        }
    }
}
