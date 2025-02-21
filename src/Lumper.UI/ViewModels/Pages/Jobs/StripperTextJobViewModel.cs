namespace Lumper.UI.ViewModels.Pages.Jobs;

using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using Lumper.Lib.Jobs;
using Lumper.Lib.Stripper;
using Lumper.UI.Services;
using Lumper.UI.Views.Pages.Jobs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public class StripperTextJobViewModel : JobViewModel
{
    public override StripperTextJob Job { get; }

    [Reactive]
    public string? Config { get; set; }

    [ObservableAsProperty]
    public bool IsConfigValid { get; }

    [ObservableAsProperty]
    public string? ErrorMessage { get; }

    public StripperTextJobViewModel(StripperTextJob job)
        : base(job)
    {
        RegisterView<StripperTextJobViewModel, StripperTextJobView>();

        Job = job;
        Config = job.Config;

        IObservable<(bool success, string? errorMessage)> parsed = this.WhenAnyValue(x => x.Config)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Select(x =>
            {
                // Try parsing the config to see if it's valid, don't care about parsed value.
                // GetBytes is an extra copy but not that big of a deal, but don't want to refactor
                // StripperConfig parsing to operate on a regular string. Perf hit is neglible.
                bool success = StripperConfig.TryParse(
                    new MemoryStream(Encoding.ASCII.GetBytes(x ?? "")),
                    out _,
                    out string? errorMessage
                );

                return (success, errorMessage);
            })
            .ObserveOn(RxApp.MainThreadScheduler);

        parsed.Select(x => x.success).ToPropertyEx(this, x => x.IsConfigValid);
        parsed.Select(x => x.errorMessage).ToPropertyEx(this, x => x.ErrorMessage);

        this.WhenAnyValue(x => x.Config).BindTo(this, x => x.Job.Config);
    }

    protected override void OnSuccess() => BspService.Instance.EntityLumpViewModel?.UpdateViewModelFromModel();
}
