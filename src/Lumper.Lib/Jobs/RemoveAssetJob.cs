namespace Lumper.Lib.Jobs;

using System.Collections.Generic;
using System.Linq;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.Util;
using NLog;

public class RemoveAssetJob : Job, IJob
{
    public static string JobName => "Remove Game Assets";
    public override string JobNameInternal => JobName;

    /// <summary>
    /// List of origins to remove matching assets from. If null, all matching assets are removed.
    /// The typical use-case for this job is removing *all* assets; if we filled this list with
    /// every current origin, existing workflows would become incomplete as if new origins were added.
    /// </summary>
    public List<string>? OriginFilter { get; set; } = [];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override bool Run(BspFile bsp)
    {
        if (OriginFilter?.Count == 0)
        {
            Logger.Warn("No games selected.");
            return false;
        }

        PakfileLump pakfileLump = bsp.GetLump<PakfileLump>();

        int numMatches = 0;

        foreach (PakfileEntry entry in pakfileLump.Entries.ToList())
        {
            string hash = entry.Hash;
            if (!AssetManifest.Manifest.TryGetValue(hash, out List<AssetManifest.Asset>? assets))
                continue;

            if (OriginFilter is not null && !assets.Any(asset => OriginFilter.Contains(asset.Origin)))
                continue;

            pakfileLump.Entries.Remove(entry);
            numMatches++;
            string matches = string.Join(", ", assets.Select(asset => $"{asset.Origin} asset {asset.Path}"));
            Logger.Info($"Removed {entry.Key} which matched {matches}");
        }

        if (numMatches > 0)
        {
            Logger.Info($"Removed {numMatches} game assets!");
            pakfileLump.IsModified = true;
            return true;
        }
        else
        {
            Logger.Info("Did not find any game assets to remove.");
            return false;
        }
    }
}
