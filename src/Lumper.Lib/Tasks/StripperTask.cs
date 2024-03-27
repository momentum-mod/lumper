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
    public override string Type => "StripperTask";

    [JsonIgnore]
    protected List<Block> Blocks { get; set; } = [];

    public string? ConfigPath { get; set; } = configPath;

    private void Load(string configPath)
    {
        ConfigPath = configPath;
        Parse(File.Open(configPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    // Expects trimmed string
    private static bool IsComment(string line) =>
        line.StartsWith(';') ||
        line.StartsWith("//", StringComparison.Ordinal) ||
        line.StartsWith('#') ||
        line == "";

    private void Parse(Stream stream)
    {
        var reader = new StreamReader(stream);
        var lineNr = 0;
        var prevBlock = "";
        while (reader.ReadLine() is { } line)
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
                _ => throw new NotImplementedException($"Unknown title '{line}' in line {lineNr}")
            };
            prevBlock = line;

            block.Parse(reader, blockOpen, ref lineNr);
            Blocks.Add(block);
        }
    }

    public override TaskResult Run(BspFile bsp)
    {
        if (!Path.Exists(ConfigPath))
            return TaskResult.Failed;

        Load(ConfigPath);
        Progress.Max = Blocks.Count;
        EntityLump entityLump = bsp.GetLump<EntityLump>();

        foreach (Block block in Blocks)
        {
            block.Apply(entityLump);
            Progress.Count++;
        }

        return TaskResult.Success;
    }
}
