namespace Lumper.Lib.Jobs;

using System;
using System.Collections.Generic;
using System.IO;
using BSP;
using BSP.Lumps.BspLumps;
using Newtonsoft.Json;
using NLog;

public partial class StripperJob(string? configPath = null) : Job, IJob
{
    public static string JobName => "Stripper";
    public override string JobNameInternal => JobName;

    [JsonIgnore]
    protected List<Block> Blocks { get; } = [];

    public string? ConfigPath { get; set; } = configPath;

    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public StripperJob() : this(null) { }

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

            Block block = line switch {
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

    public override bool Run(BspFile bsp)
    {
        if (string.IsNullOrEmpty(ConfigPath) || !Path.Exists(ConfigPath))
        {
            Logger.Warn($"Cannot load config \"{ConfigPath}\", ignoring job.");
            return false;
        }

        Load(ConfigPath);
        Progress.Max = Blocks.Count;
        EntityLump entityLump = bsp.GetLump<EntityLump>();

        foreach (Block block in Blocks)
        {
            block.Apply(entityLump);
            Progress.Count++;
        }

        return true;
    }
}
