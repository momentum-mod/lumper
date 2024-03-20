namespace Lumper.UI.ViewModels.Tasks;
using System.Collections.Generic;
using Lumper.Lib.Tasks;

/// <summary>
///     ViewModel for Stripper Task
/// </summary>
public partial class ChangeTextureTaskViewModel : TaskViewModel
{
    public class ChangeTextureReplaceItem : ChangeTextureItem
    {
        public ChangeTextureReplaceItem(ChangeTextureTaskViewModel parent,
            ChangeTextureTask task,
            KeyValuePair<string, string>? item = null)
            : base(parent, task)
        {
            if (item is not null)
            {
                Source = item.Value.Key;
                Target = item.Value.Value;
            }
        }

        public override bool Add(string? source = null,
            string? target = null)
        {
            source ??= Source;
            target ??= Target;

            if (_task.Replace.TryAdd(source, target))
            {
                return true;
            }
            return false;
        }

        protected override void Remove() => _task.Replace.Remove(Source);

        protected override void UpdateValue(string? newValue)
        {
            newValue ??= Target;
            _task.Replace[Source] = newValue;
        }

    }
}
