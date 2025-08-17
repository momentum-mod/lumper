namespace Lumper.UI.ViewModels.Pages.EntityReview;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DynamicData;
using EntityEditor;
using Lumper.Lib.EntityRules;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Entity;
using Lumper.UI.Views.Pages.EntityReview;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using EntityRules = System.Collections.Generic.Dictionary<string, Lib.EntityRules.EntityRule>;

public sealed class EntityReviewViewModel : ViewModelWithView<EntityReviewViewModel, EntityReviewView>
{
    [Reactive]
    public string? RulesFilePath { get; set; } = EntityRule.DefaultRulesPath;

    private readonly ReadOnlyObservableCollection<EntityReviewResult> _results;
    public ReadOnlyObservableCollection<EntityReviewResult> Results => _results;

    public EntityReviewViewModel()
    {
        // Map tuple of ent lumps (handles BSP changes) and rulesets (handle rule file changes)
        // into changesets of EntityViewResults, where we group every entity into a result based
        // on classname. Switch() unsubscribes the previous changeset pipeline whenever tuple
        // changes.
        this.WhenAnyValue(x => x.RulesFilePath)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(EntityRule.LoadRules)
            .CombineLatest(BspService.Instance.WhenAnyValue(x => x.EntityLumpViewModel))
            .Select(tuple =>
            {
                (EntityRules rules, EntityLumpViewModel? entLump) = tuple;
                return entLump
                        ?.Entities.Connect()
                        .AutoRefresh() // This could be a perf hit but seems okay on very entity-heavy maps...
                        .GroupWithImmutableState(y => y.Classname)
                        .Transform(y => new EntityReviewResult(rules, y.Key, y.Count))
                    ?? Observable.Return<IChangeSet<EntityReviewResult, string>>(
                        ChangeSet<EntityReviewResult, string>.Empty
                    );
            })
            .Switch()
            .SortBy(y => y)
            .Bind(out _results)
            .Subscribe();
    }

    public void SwitchToEntityEditor(string classname)
    {
        PageService.Instance.ViewPage(Page.EntityEditor);

        var entityEditor = (EntityEditorViewModel)PageService.Instance.Pages[Page.EntityEditor].Get();
        entityEditor.Filters.Classname = classname;
    }

    public async Task ShowFilePickerDialog()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        IReadOnlyList<IStorageFile> result = await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(
            GenerateFilePickerOptions()
        );

        if (result.Count == 0)
            return;

        RulesFilePath = result[0].Path.LocalPath;
    }

    private static FilePickerOpenOptions GenerateFilePickerOptions()
    {
        return new()
        {
            Title = "Pick Entity Rule File",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Entity Rule File") { Patterns = ["*.json"] }],
        };
    }

    [SuppressMessage("Design", "CA1036:Override methods on comparable types")]
    public class EntityReviewResult : IComparable
    {
        public string Classname { get; }
        public int Count { get; }
        public string Validity { get; }
        public IBrush Style { get; }
        public string Comment { get; }
        public int Level { get; }

        public EntityReviewResult(EntityRules rules, string classname, int count)
        {
            Count = count;
            Classname = classname;

            if (classname == EntityViewModel.MissingClassname)
            {
                Style = Brushes.Red;
                Comment = "Entity has no classname, so must be invalid";
                Validity = "Invalid";
                return;
            }

            EntityRule? rule = rules.GetValueOrDefault(classname);

            if (rule is null)
            {
                Style = Brushes.DeepPink;
                Comment =
                    "Entity classname is not recognised. Either it's not a valid entity, or it's missing from entity rules!";
                Validity = "Unknown";
                Level = (int)EntityRule.AllowLevel.Unknown;
                return;
            }

            Comment = rule.Comment ?? "";
            Level = (int)rule.Level;
            switch (rule.Level)
            {
                case EntityRule.AllowLevel.Allow:
                    Style = Brushes.DarkOliveGreen;
                    Validity = "Valid";
                    break;
                case EntityRule.AllowLevel.Deny:
                    Style = Brushes.Red;
                    Validity = "Invalid";
                    break;
                case EntityRule.AllowLevel.Warn:
                    Style = Brushes.OrangeRed;
                    Validity = "Warning";
                    break;
                case EntityRule.AllowLevel.Unknown:
                default:
                    Style = Brushes.DeepPink;
                    Validity = "Unknown";
                    break;
            }
        }

        public int CompareTo(object? obj)
        {
            if (obj is not EntityReviewResult other)
                return 1;

            if (Level != other.Level)
                return Level - other.Level;

            return string.Compare(Classname, other.Classname, StringComparison.Ordinal);
        }
    }
}
