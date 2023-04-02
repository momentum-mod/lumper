using ReactiveUI;
using Lumper.Lib.Tasks;

namespace Lumper.UI.ViewModels.Tasks;

/// <summary>
///     ViewModel for ChangeTextrue Task
/// </summary>
public partial class ChangeTextureTaskViewModel : TaskViewModel
{
    public abstract class ChangeTextureItem : ViewModelBase
    {
        public ChangeTextureItem(ChangeTextureTaskViewModel parent,
                                 ChangeTextureTask task)
        {
            Parent = parent;
            _task = task;
        }

        ~ChangeTextureItem()
        {
            Remove();
        }

        public ChangeTextureTaskViewModel Parent { get; }

        protected readonly ChangeTextureTask _task;

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
