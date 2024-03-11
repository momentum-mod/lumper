using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Microsoft.Extensions.Logging;

namespace Lumper.Lib.Tasks
{
    public class CompressionTask : LumperTask
    {
        public override string Type { get; } = "CompressionTask";
        public bool CompressLumps { get; set; }
        public bool CompressPakFile { get; set; }

        public CompressionTask()
        { }

        public CompressionTask(bool compressLumps)
        {
            CompressLumps = compressLumps;
        }

        public override TaskResult Run(BspFile map)
        {
            if (!CompressLumps)
            {
                //todo error message?
                return TaskResult.Failed;
            }

            Progress.Max = map.Lumps.Count;
            int i = 0;
            foreach (var lump in map.Lumps)
            {
                _logger.LogInformation($"{i} {lump.Key} {lump.Value.GetType().Name}");
                i++;

                if (lump.Value is not GameLump && lump.Value is not PakFileLump)
                {
                    lump.Value.Compress = CompressLumps;
                }
                else if (lump.Value is GameLump gameLump)
                {
                    foreach (var lump2 in gameLump.Lumps)
                    {
                        lump2.Value.Compress = CompressLumps;
                    }
                }

                Progress.Count++;
            }
            return TaskResult.Success;
        }
    }
}