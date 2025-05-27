namespace Lumper.UI.Views.BspInfo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Lib.RequiredGames;
using Lumper.Lib.EntityRules;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.BspInfo;
using Lumper.UI.ViewModels.Shared.Pakfile;
using ReactiveUI;
using EntityRules = System.Collections.Generic.Dictionary<string, Lib.EntityRules.EntityRule>;

public partial class BspInfoView : ReactiveWindow<BspInfoViewModel>
{
    public BspInfoView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            InitCompression();
            InitEntities(disposables);
            InitPakfile(disposables);
            InitRequiredGames(disposables);
        });
    }

    private void InitCompression()
    {
        int compressedLumps = BspService.Instance.CompressedLumps;
        int nonEmptyLumps = BspService.Instance.NonEmptyLumps;
        if (compressedLumps != nonEmptyLumps)
        {
            Compression.Foreground = Brushes.IndianRed;
            Compression.Text =
                compressedLumps == 0
                    ? "Uncompressed"
                    : $"Partially compressed ({compressedLumps}/{nonEmptyLumps} nonempty lumps)";
        }
        else
        {
            Compression.Foreground = Brushes.GreenYellow;
            Compression.Text = "Compressed";
        }
    }

    private void InitRequiredGames(CompositeDisposable disposables)
    {
        RequiredGamesStr.Text = "Checking required games...";

        BspService
            .Instance.WhenAnyValue(x => x.BspFile)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(bsp => bsp != null ? RequiredGames.GetRequiredGames(bsp).summary : "")
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(requiredGames => RequiredGamesStr.Text = requiredGames)
            .DisposeWith(disposables);
    }

    private void InitEntities(CompositeDisposable disposables)
    {
        Entities.Text = "Parsing entities...";
        BadEntities.Text = "";
        BspService
            .Instance.WhenAnyValue(x => x.EntityLumpViewModel)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(entLump =>
            {
                if (entLump is null)
                    return (0, 0);

                EntityRules rules = EntityRule.LoadRules(null);

                // Tuple of total entities and entities that are not allowed
                return (
                    entLump.Entities.Count,
                    entLump.Entities.Items.Count(x =>
                        !rules.TryGetValue(x.Classname, out EntityRule? rule)
                        || rule.Level is EntityRule.AllowLevel.Deny or EntityRule.AllowLevel.Unknown
                    )
                );
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(tuple =>
            {
                (int total, int bad) = tuple;

                Entities.Text = $"{total}";
                BadEntities.Text = bad > 0 ? $" ({bad} invalid entities)" : "";
            })
            .DisposeWith(disposables);
    }

    private void InitPakfile(CompositeDisposable disposables)
    {
        // Checking official assets requires reading and hashing the entire paklump, which is a
        // very expensive operation due to PrefetchData calls. Once PrefetchData has completed
        // processing is very quick, so we run the prefetches in a loop whenever the window is
        // open, cancelling whenever the window closes.

        // For sake of code simplicity not doing any reactive stuff here, not reacting to any
        // changes to pakfile lump whilst asset checking is ongoing.
        IReadOnlyList<PakfileEntryViewModel> pakfileEntries =
            BspService.Instance.PakfileLumpViewModel?.Entries.Items ?? [];

        int totalAssets = pakfileEntries.Count;
        int officialAssets = 0;
        int checkedAssets = 0;
        PakfileEntries.Text = $"{totalAssets}";

        Observable
            .Create<int>(obs =>
            {
                var cancel = new CancellationDisposable();

                Observable.Start(
                    () =>
                    {
                        foreach (PakfileEntryViewModel entry in pakfileEntries)
                        {
                            if (cancel.Token.IsCancellationRequested)
                                return;

                            entry.PrefetchData();

                            obs.OnNext(entry.MatchingGameAssets.Count > 0 ? 1 : 0);
                        }

                        obs.OnCompleted();
                    },
                    RxApp.TaskpoolScheduler
                );

                return cancel;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Finally(() => PakfileProcessing.Text = "")
            .Subscribe(matched =>
            {
                officialAssets += matched;
                checkedAssets++;
                PakfileProcessing.Text = $"[checked {checkedAssets}/{pakfileEntries.Count}] ";
                OfficialPakfileEntries.Text = officialAssets > 0 ? $"({officialAssets} official assets)" : "";
            })
            .DisposeWith(disposables);
    }

    private async void CopyText(object? _, RoutedEventArgs __)
    {
        IClipboard? clipboard = GetTopLevel(this)?.Clipboard;

        if (clipboard != null)
            await clipboard.SetTextAsync(Text?.Inlines?.Text);
    }
}
