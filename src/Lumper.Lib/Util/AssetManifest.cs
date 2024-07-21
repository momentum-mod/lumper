namespace Lumper.Lib.Util;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NLog;

public static class AssetManifest
{
    public enum Game
    {
        hl2,
        cstrike,
        csgo,
        tf,
        portal,
        portal2,
        l4d,
        l4d2,
        dod, // TODO: idk if these are right
        ep1,
        ep2,
        unknown
    }
    /// <summary>
    /// An asset from an existing Source, usually Valve assets that we want to flag/get rid of.
    /// </summary>
    public record Asset
    {
        public required Game Game { get; init; }
        public required string FileName { get; init; }

        public string GameName => GameNames[Game];
    }

    public static readonly Dictionary<Game, string> GameNames = new() {
        { Game.hl2, "HL2" },
        { Game.cstrike, "CS:S" },
        { Game.csgo, "CS:GO" },
        { Game.tf, "TF2" },
        { Game.portal, "Portal" },
        { Game.portal2, "Portal 2" },
        { Game.l4d, "L4D" },
        { Game.l4d2, "L4D2" },
        { Game.dod, "DoD" },
        { Game.ep1, "EP1" },
        { Game.ep2, "EP2" },
        { Game.unknown, "Unknown" }
    };

    private static readonly Lazy<Dictionary<string, List<Asset>>> _manifest = new(Load);

    /// <summary>
    /// Dictionary of assets loaded from the asset.manifest file, keyed by SHA1 hash of the asset.
    /// Valve games contain a surprising number of duplicate assets (i.e. matching hashes) so we store a list
    /// of assets rather than single assets.
    /// </summary>
    public static Dictionary<string, List<Asset>> Manifest => _manifest.Value;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static Dictionary<string, List<Asset>> Load()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var manifest = new Dictionary<string, List<Asset>>();

        // File of lines in format <game> <path> <SHA1 hash>
        // Example: hl2 materials\brick\brickfloor001a.vtf 5525383EB5B977DFA7C4C9612B411ECBF294600F
        // foreach (var line in File.ReadLines("./asset.cache"))
        foreach (var line in File.ReadLines("./asset.cache"))
        {
            var split = line.Split(' ');
            var hash = split[2];
            // TODO: SLOWW!!!!
            if (!Enum.TryParse(split[0], out Game game))
                game = Game.unknown;

            var asset = new Asset { Game = game, FileName = split[1] };
            if (!manifest.TryAdd(hash, [asset]))
                manifest[hash].Add(asset);
        }

        stopwatch.Stop();
        Logger.Debug($"Loaded asset manifest in {stopwatch.ElapsedMilliseconds}ms");

        return manifest;
    }
}
