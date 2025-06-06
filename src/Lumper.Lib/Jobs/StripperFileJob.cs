namespace Lumper.Lib.Jobs;

using System.IO;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Stripper;
using NLog;

public class StripperFileJob : Job, IJob
{
    public static string JobName => "Stripper (File)";
    public override string JobNameInternal => JobName;

    public string? ConfigPath { get; set; }

    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public override bool Run(BspFile bsp)
    {
        if (string.IsNullOrEmpty(ConfigPath) || !Path.Exists(ConfigPath))
        {
            Logger.Warn($"""Cannot load config "{ConfigPath}", ignoring job.""");
            return false;
        }

        using FileStream stream = File.Open(ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var config = StripperConfig.Parse(stream);

        Progress.Max = config.Blocks.Count;

        EntityLump? entityLump = bsp.GetLump<EntityLump>();
        if (entityLump == null)
        {
            Logger.Warn("No entity lump found, ignoring job.");
            return false;
        }

        foreach (StripperConfig.Block block in config.Blocks)
        {
            block.Apply(entityLump);
            Progress.Count++;
        }

        return true;
    }
}
