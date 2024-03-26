namespace Lumper.Lib.Tasks;
using System;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;

public class CompressionTask(bool compressLumps) : LumperTask
{
    public override string Type { get; } = "CompressionTask";
    public bool CompressLumps { get; set; }
    public bool CompressLumps { get; set; } = compressLumps;
    public bool CompressPakFile { get; set; }

    public CompressionTask()
    { }

    public CompressionTask(bool compressLumps) => CompressLumps = compressLumps;

    public override TaskResult Run(BspFile bsp)
    {
        if (!CompressLumps)
        {
            // TODO: log error
            return TaskResult.Failed;
        }

        var i = 0;
        Progress.Max = bsp.Lumps.Count;
        foreach (Lump<BspLumpType>? lump in bsp.Lumps.Values)
        {
            Console.WriteLine($"{i} {lump.Key} {lump.Value.GetType().Name}");
            i++;

            if (lump is not GameLump and not PakFileLump)
            {
                lump.Compress = CompressLumps;
            }
            else if (lump is GameLump gameLump)
            {
                foreach (Lump lump in gameLump.Lumps.Values.OfType<Lump>())
                {
                    lump.Compress = CompressLumps;
                }
            }

            Progress.Count++;
        }

        return TaskResult.Success;
    }
}
