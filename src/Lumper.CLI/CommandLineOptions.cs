namespace Lumper.CLI;

using CommandLine;

[Flags]
public enum JsonOptions
{
    SortLumps = 0x1,
    SortProperties = 0x2,
    IgnoreOffset = 0x4,
}

public class CommandLineOptions
{
    [Value(index: 0, Required = true, MetaName = "Input BSP", HelpText = "Path to input BSP file")]
    public required string InputPath { get; set; }

    [Option(
        'v',
        "verbose",
        Default = false,
        Required = false,
        HelpText = "Enable verbose output (include debug messages)"
    )]
    public bool Verbose { get; set; }

    [Option(
        'o',
        "output",
        Required = false,
        HelpText = "Path to output BSP file. If not provided, and some action modifies the BSP, the input BSP will be overwritten."
    )]
    public string? OutputPath { get; set; }

    [Option('c', "compress", SetName = "compress", Required = false, HelpText = "Save the output BSP file compressed.")]
    public bool Compress { get; set; }

    [Option(
        'C',
        "noCompress",
        SetName = "compress",
        Required = false,
        HelpText = "Save the output BSP file uncompressed."
    )]
    public bool DontCompress { get; set; }

    [Option(
        'B',
        "skipBackup",
        Required = false,
        Default = false,
        HelpText = "Don't make a backup of the input BSP file before overwriting it."
    )]
    public bool SkipBackup { get; set; }

    [Option(
        'w',
        "workflow",
        Required = false,
        HelpText = "Path to a JSON file containing a job workflow to apply to the BSP file."
    )]
    public string? JobWorkflow { get; set; }

    [Option(
        'j',
        "json",
        Default = false,
        Required = false,
        HelpText = "Output JSON summary of the BSP to <directory containing BSP file>/<bsp file name>.json"
    )]
    public bool JsonDump { get; set; }

    [Option(
        'J',
        "jsonPath",
        Required = false,
        HelpText = "Output JSON summary of the BSP to the given path, relative to current directory. If given, --json can be omitted."
    )]
    public string? JsonPath { get; set; }

    [Option(
        'k',
        "jsonOpts",
        Default = JsonOptions.SortLumps | JsonOptions.SortProperties | JsonOptions.IgnoreOffset,
        Required = false,
        HelpText = "Provide either flags (1 = Sort Lumps, 2 = Sort Properties, 4 = Ignore Offsets),"
            + " or comma-separator list of names, e.g. sortproperties,ignoreoffsets."
    )]
    public JsonOptions JsonOptions { get; set; }

    [Option(
        'u',
        "renameMapFiles",
        Default = true,
        Required = false,
        HelpText = "Rename cubemap textures to match the BSP file name, if the name has changed. May make saving a lot slower!"
    )]
    public bool RenameMapFiles { get; set; } = true;

    [Option(
        'a',
        "check-assets",
        Default = false,
        Required = false,
        HelpText = "[VALIDATOR] Check whether the paklump contains official Valve assets, exits with 1 if so."
    )]
    public bool CheckAssets { get; set; }

    [Option(
        'r',
        "entityRules",
        Required = false,
        HelpText = "[VALIDATOR] Check the BSP file against default set of entity rules (EntityRules_Momentum.json)."
    )]
    public bool ValidateEntityRules { get; set; }

    [Option(
        'R',
        "customEntityRules",
        Required = false,
        HelpText = "[VALIDATOR] Check the BSP file against a set of entity rules at a given path."
    )]
    public string? CustomEntityRules { get; set; }
}
