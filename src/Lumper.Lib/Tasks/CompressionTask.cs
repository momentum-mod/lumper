namespace Lumper.Lib.Tasks;
using System;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps.BspLumps;

public class CompressionTask : LumperTask
{
    public override string Type { get; } = "CompressionTask";
    public bool CompressLumps { get; set; }
    public bool CompressPakFile { get; set; }

    public CompressionTask()
    { }

    public CompressionTask(bool compressLumps) => CompressLumps = compressLumps;

    public override TaskResult Run(BspFile map)
    {
        if (!CompressLumps)
        {
            // TODO: log error
            return TaskResult.Failed;
        }

        Progress.Max = map.Lumps.Count;
        var i = 0;
        foreach (System.Collections.Generic.KeyValuePair<BspLumpType, BSP.Lumps.Lump<BspLumpType>> lump in map.Lumps)
        {
            Console.WriteLine($"{i} {lump.Key} {lump.Value.GetType().Name}");
            i++;

            if (lump.Value is not GameLump and not PakFileLump)
            {
                lump.Value.Compress = CompressLumps;
            }
            else if (lump.Value is GameLump gameLump)
            {
                foreach (System.Collections.Generic.KeyValuePair<BSP.Lumps.GameLumps.GameLumpType, BSP.Lumps.Lump> lump2 in gameLump.Lumps)
                {
                    lump2.Value.Compress = CompressLumps;
                }
            }

            Progress.Count++;
        }

        return TaskResult.Success;
    }
}
