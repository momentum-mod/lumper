namespace Lumper.Lib.RequiredGames;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.AssetManifest;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Lumps.GameLumps;
using Lumper.Lib.Bsp.Struct;

public static class RequiredGames
{
    public class PackedAsset
    {
        public required string Type { get; init; }

        public required string Path { get; init; }

        private readonly List<string> _games = [];
        public required List<string> Games
        {
            get => _games;
            init
            {
                List<string> priorityGames = AssetManifest.RenamedOriginPriority;
                _games = value
                    .OrderBy(game =>
                    {
                        // First sort: ascending by index in priorityGames, if not found, otherwise use priorityGames.Count
                        // to stick everything else at the end.
                        int priority = priorityGames.IndexOf(game);
                        if (priority != -1)
                            return priority;

                        return priorityGames.Count;
                    })
                    .ThenByDescending(game =>
                    {
                        // Second sort: descending by how many times the game occurs.
                        int priority = priorityGames.IndexOf(game);
                        if (priority == -1)
                            return 0;

                        return priorityGames.Count + value.Count(asset => asset.Contains(asset));
                    })
                    .ToList();
            }
        }

        public string GamesString => string.Join(", ", Games);
    }

    public static (List<PackedAsset> assets, string summary) GetRequiredGames(BspFile? bspFile)
    {
        if (bspFile == null)
            return ([], "N/A");

        var matches = new List<PackedAsset>();
        matches.AddRange(GetMatchingTexData(bspFile));
        matches.AddRange(GetMatchingModelData(bspFile));

        // Unique Games should be collection of games that *have* to be installed, i.e.
        // no other collection of games exists that for every RequiredGame in Results,
        // at least one game in the collection is in that RequiredGame
        var minimumSet = new HashSet<string>();
        var uniqueGames = matches.SelectMany(r => r.Games).Distinct().ToHashSet();

        // Prioritize HL2 above all else
        if (uniqueGames.Contains("hl2"))
            minimumSet.Add("hl2");

        // For all uniqueGames, if there's a game for which at least one asset includes *only* that game,
        // add that game to the minimum set.
        foreach (string game in uniqueGames)
        {
            if (matches.Any(asset => asset.Games.Count == 1 && asset.Games.Contains(game)))
                minimumSet.Add(game);
        }

        // For all remaining uniqueGames, sort them by how many times they appear in the matches
        var ordered = uniqueGames
            .OrderByDescending(game => matches.Count(asset => asset.Games.Contains(game)))
            .ThenBy(game => game) // Sort alphabetically as a tiebreaker
            .ToList();

        // For each game in the ordered list, if it appears in any of the matches, add it, unless for every match,
        // at least one game in the minimum set is already included.
        foreach (string game in ordered)
        {
            if (
                matches.Any(asset =>
                    asset.Games.Contains(game) && !minimumSet.Any(minGame => asset.Games.Contains(minGame))
                )
            )
                minimumSet.Add(game);
        }

        string str = minimumSet.Count == 0 ? "N/A" : string.Join(", ", minimumSet);

        return (matches, str);
    }

    private static List<PackedAsset> GetMatchingTexData(BspFile bspFile)
    {
        List<PackedAsset> matches = [];

        TexDataLump texDataLump =
            bspFile.GetLump<TexDataLump>() ?? throw new InvalidDataException("TexData lump not found");

        // This probably hasn't loaded yet, make sure we do in TP thread.
        Dictionary<string, List<string>> pathManifest = AssetManifest.PathManifest;

        foreach (TexData texData in texDataLump.Data)
        {
            pathManifest.TryGetValue(
                $"materials/{texData.TexName.ToLowerInvariant()}.vmt",
                out List<string>? matchingOrigins
            );

            if (matchingOrigins == null)
                continue;

            matches.Add(
                new PackedAsset
                {
                    Type = "Texture",
                    Path = texData.TexName,
                    Games = matchingOrigins,
                }
            );
        }

        return matches;
    }

    private static List<PackedAsset> GetMatchingModelData(BspFile bspFile)
    {
        List<PackedAsset> matches = [];

        List<string>? props = bspFile.GetLump<GameLump>()?.GetLump<Sprp>()?.StaticPropsDict?.Data;

        if (props == null)
            return [];

        Dictionary<string, List<string>> pathManifest = AssetManifest.PathManifest;

        foreach (string prop in props)
        {
            pathManifest.TryGetValue(prop, out List<string>? matchingOrigins);

            if (matchingOrigins == null)
                continue;

            matches.Add(
                new PackedAsset
                {
                    Type = "Static Prop",
                    Path = prop,
                    Games = matchingOrigins,
                }
            );
        }

        return matches;
    }

    // TODO: Don't have support for searching pakfile entries or entities. Probably possible to expose static methods
    // from PakfileLump.Refactoring but it's complex and only works for a small proportion of cases.
}
