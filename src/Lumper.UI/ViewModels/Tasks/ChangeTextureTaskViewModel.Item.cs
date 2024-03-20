namespace Lumper.UI.ViewModels.Tasks;
using Lumper.Lib.Tasks;
using ReactiveUI;

/// <summary>
///     ViewModel for ChangeTextrue Task
/// </summary>
public partial class ChangeTextureTaskViewModel : TaskViewModel
{
    public abstract class ChangeTextureItem(ChangeTextureTaskViewModel parent,
                             ChangeTextureTask task) : ViewModelBase
    {
        ~ChangeTextureItem()
        {
            Remove();
        }

        public ChangeTextureTaskViewModel Parent { get; } = parent;

        protected readonly ChangeTextureTask _task = task;

        protected string _source = "";
        public string Source
        {
            get => _source;
            set
            {
                if (value != _source && value is not null)
                {
                    Remove();
                    _source = value;
                    Add(Source, Target);

                    this.RaisePropertyChanged();
                }
            }
        }

        protected string _target = "";
        public string Target
        {
            get => _target;
            set
            {
                if (value != _target && value is not null)
                {
                    UpdateValue(value);
                    _target = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public abstract bool Add(string? source = null,
                                 string? target = null);

        protected abstract void Remove();

        protected abstract void UpdateValue(string? newValue);
    }
}
