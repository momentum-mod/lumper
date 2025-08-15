namespace Lumper.Lib.Jobs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.AssetManifest;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Lumps.GameLumps;
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

    /// <summary>
    /// Whether to remove static prop lump entries for props that were removed.
    ///
    /// NOTE: Disabled for now since the collision of these props can be essential to gameplay.
    /// In the future, we'll be including outlined collision meshes in the place of the original props
    /// for official Valve assets.
    /// </summary>
    public bool RemoveStaticProps { get; set; } = false;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override bool Run(BspFile bsp)
    {
        if (OriginFilter is { Count: 0 })
        {
            Logger.Warn("No games selected.");
            return false;
        }

        PakfileLump? pakfileLump = bsp.GetLump<PakfileLump>();

        if (pakfileLump == null)
        {
            Logger.Warn("BSP does not contain a pakfile lump, ignoring job.");
            return false;
        }

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

            if (RemoveStaticProps && Path.GetExtension(entry.Key).Equals(".mdl", StringComparison.OrdinalIgnoreCase))
                RemoveStaticProp(bsp, entry.Key);

            string matches = string.Join(", ", assets.Select(asset => $"{asset.Origin} asset {asset.Path}"));

            AssetManifest.Asset? bestAsset = null;
            if (assets.Count > 1)
            {
                foreach (string origin in AssetManifest.RenamedOriginPriority)
                {
                    bestAsset = assets.FirstOrDefault(asset => asset.Origin == origin);
                    if (bestAsset != null)
                        break;
                }
            }

            bestAsset ??= assets[0];

            if (
                entry.Key != bestAsset.Path
                // Occasionally maps pack an "env_cubemap" material that matches random official assets,
                // don't refactor paths as we could replace values of the $envmap property...
                // (https://github.com/momentum-mod/lumper/issues/187)
                && !Path.GetFileName(entry.Key).Equals("env_cubemap.vtf", StringComparison.OrdinalIgnoreCase)
            )
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

    private static void RemoveStaticProp(BspFile bsp, string path)
    {
        Sprp? sprp = bsp.GetLump<GameLump>()?.GetLump<Sprp>();
        if (sprp is null)
            return;

        int idx =
            sprp.StaticPropsDict?.Data.FindIndex(name => name.Equals(path, StringComparison.OrdinalIgnoreCase)) ?? -1;

        if (idx == -1)
            return;

        int removed = sprp.StaticProps?.Data.RemoveAll(x => x.PropType == idx) ?? 0;

        if (removed > 0)
            Logger.Info($"Removed {removed} static props for {path}. This may affect lighting!");
    }
}
