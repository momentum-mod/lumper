namespace Lumper.UI.ViewModels.Pages.Jobs;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using Lumper.Lib.Jobs;
using Lumper.UI.Views.Pages.Jobs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public sealed class ReplaceTextureJobViewModel : JobViewModel, IDisposable
{
    private readonly SourceList<ReplacerViewModel> _source = new();
    private readonly ReadOnlyObservableCollection<ReplacerViewModel> _replacers;
    public ReadOnlyObservableCollection<ReplacerViewModel> Replacers => _replacers;

    public override ReplaceTextureJob Job { get; }

    public ReplaceTextureJobViewModel(ReplaceTextureJob job)
        : base(job)
    {
        RegisterView<ReplaceTextureJobViewModel, ReplaceTextureJobView>();

        Job = job;

        _source.Edit(_ =>
            _source.AddRange(
                job.Replacers.Select(x => new ReplacerViewModel
                {
                    ReplaceWith = x.ReplaceWith,
                    Matcher = x.Matcher,
                    IsRegex = x.IsRegex,
                    Model = x,
                })
            )
        );

        _source.Connect().Bind(out _replacers).Subscribe();

        _source.Connect().Transform(x => x.Model).ToCollection().Subscribe(x => job.Replacers = [.. x]);
    }

    public void Add() => _source.Add(new ReplacerViewModel());

    public void Delete(ReplacerViewModel replacer) => _source.Remove(replacer);

    public class ReplacerViewModel : ViewModel
    {
        [Reactive]
        public string Matcher { get; set; } = "";

        [Reactive]
        public string ReplaceWith { get; set; } = "";

        [Reactive]
        public bool IsRegex { get; set; }

        public ReplaceTextureJob.Replacer Model { get; init; }

        public ReplacerViewModel()
        {
            Model = new ReplaceTextureJob.Replacer();
            this.WhenAnyValue(x => x.Matcher).BindTo(this, x => x.Model.Matcher);
            this.WhenAnyValue(x => x.ReplaceWith).BindTo(this, x => x.Model.ReplaceWith);
            this.WhenAnyValue(x => x.IsRegex).BindTo(this, x => x.Model.IsRegex);
        }
    }

    public void Dispose() => _source.Dispose();
}
