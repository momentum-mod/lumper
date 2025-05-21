namespace Lumper.UI.ViewModels.Pages.RequiredGames;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lumper.Lib.AssetManifest;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Services;
using Lumper.UI.Views.Pages.RequiredGames;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public class RequiredGame
{
    public required string Type { get; set; }

    public required string Path { get; set; }

    public required List<string> Games { get; set; }

    public string GamesString => string.Join(", ", Games);
}

public sealed class RequiredGamesViewModel : ViewModelWithView<RequiredGamesViewModel, RequiredGamesView>, IDisposable
{
    private readonly Subject<Unit> _recomputer = new();

    [Reactive]
    public List<RequiredGame> Results { get; set; } = [];

    [Reactive]
    public string Required { get; set; } = "";

    public RequiredGamesViewModel()
    {
        // TODO: reactivity wrong
        BspService
            .Instance.WhenAnyValue(x => x.BspFile)
            .Where(x => x != null)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .CombineLatest(_recomputer)
            .Select(_ =>
            {
                // TODO: Props
                return GetMatchingTexData();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(data =>
            {
                Results = data;
                // Unique Games should be collection of games that *have* to be installed, i.e.
                // no other collection of games exists that for every RequiredGame in Results,
                // at least one game in the collection is in that RequiredGame
                var uniqueGames = Results.SelectMany(r => r.Games).Distinct().ToHashSet();
                var minimumSet = new HashSet<string>();
                var remaining = Results.ToList();

                while (remaining.Count != 0)
                {
                    string bestGame = uniqueGames
                        .OrderByDescending(game => remaining.Count(req => req.Games.Contains(game)))
                        .First();

                    minimumSet.Add(bestGame);
                    remaining.RemoveAll(req => req.Games.Contains(bestGame));
                }

                Required = string.Join(", ", minimumSet);
            });

        Recompute();
    }

    public static List<RequiredGame> GetMatchingTexData()
    {
        List<RequiredGame> matches = new();

        TexDataLump texDataLump =
            BspService.Instance.BspFile?.GetLump<TexDataLump>()
            ?? throw new InvalidDataException("TexData lump not found");

        // This probably hasn't loaded yet, make sure we do in TP thread.
        Dictionary<string, List<string>> pathManifest = AssetManifest.PathManifest;

        foreach (TexData texData in texDataLump.Data)
        {
            pathManifest.TryGetValue(
                $"materials/{texData.TexName.ToLowerInvariant()}.vmt",
                out List<string>? matchingOrigins
            );

            // If HL2 just skip, should never matter.
            if (matchingOrigins == null || matchingOrigins.Contains("hl2"))
                continue;

            matches.Add(
                new RequiredGame
                {
                    Type = "Texture",
                    Path = texData.TexName,
                    Games = matchingOrigins,
                }
            );
        }

        return matches;
    }

    public void Recompute()
    {
        _recomputer.OnNext(Unit.Default);
    }

    public void Dispose()
    {
        _recomputer.Dispose();
    }
}
