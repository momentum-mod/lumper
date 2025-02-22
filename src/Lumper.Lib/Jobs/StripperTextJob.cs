namespace Lumper.Lib.Jobs;

using System.IO;
using System.Text;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Stripper;
using NLog;

public class StripperTextJob : Job, IJob
{
    public static string JobName => "Stripper (Text)";
    public override string JobNameInternal => JobName;

    public string? Config { get; set; }

    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public override bool Run(BspFile bsp)
    {
        if (string.IsNullOrWhiteSpace(Config))
        {
            Logger.Warn("Empty config, ignoring job.");
            return false;
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Config));
        var config = StripperConfig.Parse(stream);

        Progress.Max = config.Blocks.Count;
        EntityLump entityLump = bsp.GetLump<EntityLump>();

        foreach (StripperConfig.Block block in config.Blocks)
        {
            block.Apply(entityLump);
            Progress.Count++;
        }

        return true;
    }
}
