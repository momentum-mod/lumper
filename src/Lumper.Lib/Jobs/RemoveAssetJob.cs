namespace Lumper.Lib.Jobs;

using System.Collections.Generic;
using System.Linq;
using BSP;
using BSP.Lumps.BspLumps;
using BSP.Struct;
using NLog;
using Util;

public class RemoveAssetJob : Job, IJob
{
    public static string JobName => "Remove Game Assets";
    public override string JobNameInternal => JobName;

    public List<AssetManifest.Game> Games { get; set; } = [];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override bool Run(BspFile bsp)
    {
        if (Games.Count == 0)
        {
            Logger.Warn("No games selected.");
            return false;
        }

        PakfileLump pakfileLump = bsp.GetLump<PakfileLump>();

        var numMatches = 0;

        // Could probably speed this up a bit by parallelizing, but we can't read
        // multiple zip entries at a time, and that's the most expensive operation here.
        foreach (PakfileEntry entry in pakfileLump.Entries.ToList())
        {
            var hash = entry.HashSHA1;
            if (!AssetManifest.Manifest.TryGetValue(hash, out List<AssetManifest.Asset>? assets) ||
                !assets.Any(asset => Games.Contains(asset.Game)))
                continue;

            pakfileLump.Entries.Remove(entry);
            numMatches++;
            Logger.Info(
                $"Removed {entry.Key} which matched {string.Join(", ", assets.Select(asset => $"{asset.GameName} asset {asset.FileName}"))}");
        }

        if (numMatches > 0)
        {
            Logger.Info($"Removed {numMatches} game assets!");
            return true;
        }
        else
        {
            Logger.Info("Did not find any game assets to remove.");
            return false;
        }
    }
}
