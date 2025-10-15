namespace Lumper.Lib.Jobs;

using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using NLog;

public class AddSkyOcclusionFlagJob : Job, IJob
{
    public static string JobName => "Add Sky Occlusion Flag";
    public override string JobNameInternal => JobName;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override bool Run(BspFile bsp)
    {
        TexInfoLump? texinfos = bsp.GetLump<TexInfoLump>();

        if (texinfos == null)
        {
            Logger.Error("BSP without texinfos?");
            return false;
        }

        int count = 0;
        foreach (TexInfo texinfo in texinfos.Data)
        {
            if ((texinfo.Flags & (SurfaceFlag.Sky | SurfaceFlag.Sky2d)) == 0)
                continue;
            texinfo.Flags |= SurfaceFlag.SkyOcclusion;
            count++;
        }

        Logger.Info($"Added {count} occlusion flags to sky texture(s).");
        return count != 0;
    }
}
