namespace Lumper.Lib.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Newtonsoft.Json;

// Change entities using stripper config
public partial class StripperTask(string? configPath) : LumperTask
{
    public override string Type { get; } = "StripperTask";

    [JsonIgnore]
    protected List<Block> blocks = [];

    public string? ConfigPath { get; set; } = configPath;

    public void Load(string configPath)
    {
        ConfigPath = configPath;
        Parse(File.Open(configPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    ///expects trimmed string
    protected static bool IsComment(string line) => line.StartsWith(";")
                                                    || line.StartsWith("//")
                                                    || line.StartsWith("#")
                                                    || line == "";

    public void Parse(Stream stream)
    {
        var reader = new StreamReader(stream);

        string line;
        var lineNr = 0;
        var prevBlock = "";
        while ((line = reader.ReadLine()) != null)
        {
            lineNr++;

            line = line.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var blockOpen = false;
            if (line == "{")
            {
                line = prevBlock;
                blockOpen = true;
            }
            else if (IsComment(line))
            {
                continue;
            }

            Block block = line switch
            {
                "filter:" or "remove:" => new Filter(),
                "add:" => new Add(),
                "modify:" => new Modify(),
                _ => throw new NotImplementedException($"Unknown title '{line}' in line {lineNr}"),
            };
            prevBlock = line;

            block.Parse(reader, blockOpen, ref lineNr);
            blocks.Add(block);
        }
    }

    public override TaskResult Run(BspFile bsp)
    {
        if (!Path.Exists(ConfigPath))
            return TaskResult.Failed;

        Load(ConfigPath);
        Progress.Max = blocks.Count;
        EntityLump entityLump = bsp.GetLump<EntityLump>();

        foreach (Block block in Blocks)
        {
            block.Apply(entityLump);
            Progress.Count++;
        }

        return TaskResult.Success;
    }
}
