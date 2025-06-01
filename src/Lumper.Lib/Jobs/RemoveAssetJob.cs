namespace Lumper.Lib.Jobs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.AssetManifest;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
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

    /// <summary>
    /// Whether to skip removing VMT files. These are notoriously problematic and would require
    /// MDL file parsing to get working for props.
    /// </summary>
    public bool SkipVmts { get; set; } = true;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Origins we prioritise when renaming assets that match a hash but not the original filename,
    /// or there's multiple officials with the same hash, where one path matches but a more
    /// accessible game has the same asset with a different path.
    /// In both case, we update references to the most accessible path everywhere to avoid missing
    /// textures as best we can. This is biased towards Momentum!
    /// </summary>
    private static readonly string[] RenamedOriginPriority = ["hl2", "tf2", "css", "csgo"];

    public override bool Run(BspFile bsp)
    {
        if (OriginFilter is { Count: 0 })
        {
            Logger.Warn("No games selected.");
            return false;
        }

        PakfileLump pakfileLump = bsp.GetLump<PakfileLump>();

        Logger.Info("Removing game assets... this may take a while!");
        int totalMatches = 0;

        var entries = pakfileLump.Entries.ToList();
        Progress.Max = entries.Count;
        foreach (PakfileEntry entry in entries)
        {
            Progress.Count++;

            if (SkipVmts && Path.GetExtension(entry.Key).Equals(".vmt", StringComparison.OrdinalIgnoreCase))
                continue;

            string hash = entry.Hash;
            if (!AssetManifest.Manifest.TryGetValue(hash, out List<AssetManifest.Asset>? assets))
                continue;

            if (assets.Count == 0)
                continue;

            if (OriginFilter is not null && !assets.Any(asset => OriginFilter.Contains(asset.Origin)))
                continue;

            pakfileLump.Entries.Remove(entry);
            totalMatches++;
            string matches = string.Join(", ", assets.Select(asset => $"{asset.Origin} asset {asset.Path}"));

            AssetManifest.Asset? bestAsset = null;
            if (assets.Count > 1)
            {
                foreach (string origin in RenamedOriginPriority)
                {
                    bestAsset = assets.FirstOrDefault(asset => asset.Origin == origin);
                    if (bestAsset != null)
                        break;
                }
            }

            bestAsset ??= assets[0];

            if (entry.Key != bestAsset.Path)
            {
                pakfileLump.UpdatePathReferences(entry.Key, bestAsset.Path);
                Logger.Info($"Removed {entry.Key} which matched {matches}, updating references to {bestAsset.Path}");
            }
            else
            {
                Logger.Info($"Removed {entry.Key} which matched {matches}");
            }
        }

        if (totalMatches > 0)
        {
            Logger.Info($"Removed {totalMatches} game assets!");
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
