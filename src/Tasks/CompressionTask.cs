using System;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps.BspLumps;

namespace Lumper.Tasks
{
    public class CompressionTask : LumperTask
    {
        public override string Type { get; } = "CompressionTask";
        public bool CompressLumps { get; set; }
        public bool CompressPakFile { get; set; }
        public CompressionTask(bool compressLumps, bool compressPakFile)
        {
            CompressLumps = compressLumps;
            CompressPakFile = compressPakFile;
        }
        public override TaskResult Run(BspFile map)
        {
            if (!CompressLumps && !CompressPakFile)
            {
                //todo error message?
                return TaskResult.Failed;
            }

            Progress.Max = map.Lumps.Count;
            int i = 0;
            foreach (var lump in map.Lumps)
            {
                Console.WriteLine($"{i} {lump.Key} {lump.Value.GetType().Name}");
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
                else if (CompressPakFile && lump.Value is PakFileLump pakFileLump)
                {
                    var zip = pakFileLump.GetZipArchive();
                    pakFileLump.SetZipArchive(zip, true);
                }

                Progress.Count++;
            }
            return TaskResult.Success;
        }
    }
}