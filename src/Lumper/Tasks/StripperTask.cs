using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps.BspLumps;

namespace Lumper.Lib.Tasks
{
    //change entities wth stripper config
    public partial class StripperTask : LumperTask
    {
        public override string Type { get; } = "StripperTask";

        [JsonIgnore()]
        protected List<Block> blocks = new();
        public string ConfigPath { get; set; }
        public StripperTask(string configPath)
        {
            ConfigPath = configPath;
            Parse(File.Open(configPath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        ///expects trimmed string
        protected static bool IsComment(string line)
        {
            return line.StartsWith(";")
                     || line.StartsWith("//")
                     || line.StartsWith("#")
                     || line == "";
        }

        public void Parse(Stream stream)
        {
            var reader = new StreamReader(stream);

            string line;
            int lineNr = 0;
            string prevBlock = "";
            while ((line = reader.ReadLine()) != null)
            {
                lineNr++;

                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                Block block;

                bool blockOpen = false;
                if (line == "{")
                {
                    line = prevBlock;
                    blockOpen = true;
                }
                else if (IsComment(line))
                    continue;

                switch (line)
                {
                    case "filter:":
                    case "remove:":
                        block = new Filter();
                        break;
                    case "add:":
                        block = new Add();
                        break;
                    case "modify:":
                        block = new Modify();
                        break;
                    default:
                        throw new NotImplementedException($"Unknown title '{line}' in line {lineNr}");
                }
                prevBlock = line;

                block.Parse(reader, blockOpen, ref lineNr);
                blocks.Add(block);
            }
        }

        public override TaskResult Run(BspFile map)
        {
            Progress.Max = blocks.Count;
            var entityLump = map.GetLump<EntityLump>();
            foreach (var block in blocks)
            {
                block.Apply(entityLump);
                Progress.Count++;
            }
            return TaskResult.Success;
        }
    }
}