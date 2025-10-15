namespace Lumper.UI.ViewModels.Pages.Jobs;

using Lumper.Lib.Jobs;
using Lumper.UI.Views.Pages.Jobs;

public sealed class AddSkyOcclusionFlagJobViewModel : JobViewModel
{
    public override AddSkyOcclusionFlagJob Job { get; }

    public AddSkyOcclusionFlagJobViewModel(AddSkyOcclusionFlagJob job)
        : base(job)
    {
        RegisterView<AddSkyOcclusionFlagJobViewModel, AddSkyOcclusionFlagJobView>();

        Job = job;
    }
}
