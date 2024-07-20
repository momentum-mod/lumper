namespace Lumper.UI.ViewModels.Pages.Jobs;

using System;
using System.Collections.Generic;
using System.Linq;
using Lib.BSP.Lumps.BspLumps;
using Lib.Jobs;
using Lib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;
using Shared.Pakfile;
using Views.Pages.Jobs;

public sealed class RemoveAssetJobViewModel : JobViewModel
{
    public class GameSelection : ReactiveObject
    {
        public required AssetManifest.Game Game { get; init; }

        public string Name => AssetManifest.GameNames[Game];

        [Reactive]
        public bool Selected { get; set; }
    }

    public List<GameSelection> Selection { get; } =
        Enum.GetValues<AssetManifest.Game>()
            .Select(game => new GameSelection { Game = game, Selected = true }).ToList();

    public override RemoveAssetJob Job { get; }

    public RemoveAssetJobViewModel(RemoveAssetJob job) : base(job)
    {
        RegisterView<RemoveAssetJobViewModel, RemoveAssetJobView>();

        Job = job;

        foreach (GameSelection selection in Selection)
            selection.WhenAnyValue(x => x.Selected)
                .Subscribe(_ =>
                    Job.Games = Selection
                        .Where(x => x.Selected)
                        .Select(x => x.Game)
                        .ToList());
    }

    protected override void OnSuccess()
        => BspService.Instance.ResetLumpViewModel(nameof(PakfileLumpViewModel));
}
