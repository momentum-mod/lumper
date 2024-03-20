namespace Lumper.UI.ViewModels.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lumper.Lib.Tasks;

/// <summary>
///     ViewModel for ChangeTextrue Task
/// </summary>
public partial class ChangeTextureTaskViewModel : TaskViewModel
{
    public class ChangeTextureRegexItem : ChangeTextureItem
    {
        public ChangeTextureRegexItem(ChangeTextureTaskViewModel parent,
                                      ChangeTextureTask task,
                                      KeyValuePair<Regex, string>? item = null)
            : base(parent, task)
        {
            if (item is not null)
            {
                _source = item.Value.Key.ToString();
                _target = item.Value.Value;
            }
        }

        public override bool Add(string? source = null,
                                 string? target = null)
        {
            source ??= Source;
            target ??= Target;

            _task.ReplaceRegex.Add(
                new KeyValuePair<Regex, string>(new Regex(source), target));
            return true;
        }

        protected override void Remove()
        {
            var idx = _task.ReplaceRegex.FindIndex(
                x => x.Key.ToString() == Source
                    && x.Value == Target);
            if (idx > 0)
                _task.ReplaceRegex.RemoveAt(idx);
        }

        protected override void UpdateValue(string? newValue)
        {
            Remove();
            Add(Source, newValue);
        }
    }
}
