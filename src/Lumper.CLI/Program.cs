namespace Lumper.CLI;

using System.Diagnostics;
using CommandLine;
using CommandLine.Text;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.Lib.Jobs;
using Lumper.Lib.Util;
using NLog;
using NLog.Config;
using NLog.Targets;

internal sealed class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static bool _shouldWriteOutput;

    public static int Main(string[] args)
    {
        LoggingRule? consoleRule = LogManager.Configuration?.LoggingRules?.FirstOrDefault(rule =>
            rule.Targets.FirstOrDefault() is ColoredConsoleTarget
        );
        if (LogManager.Configuration is null || consoleRule is null)
        {
            Console.Error.WriteLine(
                "Could not find logging configuration! You should have a NLog.config in the same directory as this executable."
            );
            return 1;
        }

        // Remove date and other crap from layout. This is good for file logs but here
        // we want to just print messages directly.
        // https://github.com/NLog/NLog/wiki/Layouts
        consoleRule.Targets.OfType<ColoredConsoleTarget>().First().Layout = "${message:withException=true}";

        ParserResult<CommandLineOptions> parserResult = new Parser(with =>
        {
            with.CaseInsensitiveEnumValues = true;
            with.HelpWriter = null;
            with.AutoHelp = true;
            with.AutoVersion = true;
        }).ParseArguments<CommandLineOptions>(args);

        if (parserResult.Errors.Any())
            return ShowHelp(parserResult);

        CommandLineOptions options = parserResult.Value;

        consoleRule.SetLoggingLevels(options.Verbose ? LogLevel.Debug : LogLevel.Info, LogLevel.Fatal);
        try
        {
            return Run(options) ? 0 : 1;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return 1;
        }
    }

    // The main application logic of the program, called once the command line options have been parsed.
    //
    // It can throw at any point, and the exception will be caught and program will exit with non-zero
    // status code.
    //
    // If method completes successfully, the program will exit with status code 0.
    private static bool Run(CommandLineOptions options)
    {
        BspFile bspFile =
            BspFile.FromPath(options.InputPath, null) ?? throw new InvalidDataException("Failed to load BSP file");

        // If this is ever true, the application is being used to perform validations, then exit with 0 or 1.
        bool inValidationMode = false;
        bool validationSuccess = true;

        if (options.CheckAssets)
        {
            inValidationMode = true;
            if (!CheckAssets(bspFile))
                validationSuccess = false;
        }

        if (options.CustomEntityRules is not null)
        {
            inValidationMode = true;
            if (!ValidateEntityRules(bspFile, options.CustomEntityRules))
                validationSuccess = false;
        }
        else if (options.ValidateEntityRules)
        {
            inValidationMode = true;
            if (!ValidateEntityRules(bspFile, null))
                validationSuccess = false;
        }

        if (inValidationMode)
        {
            if (validationSuccess)
                Logger.Info("All validations passed");
            else
                Logger.Error("One or more validations failed!");

            return validationSuccess;
        }

        if (options.JobWorkflow is { } workflowPath)
            RunWorkflow(bspFile, workflowPath);

        if (_shouldWriteOutput || options.Compress || options.DontCompress)
        {
            DesiredCompression compression = DesiredCompression.Unchanged;
            if (options.Compress)
                compression = DesiredCompression.Compressed;
            else if (options.DontCompress)
                compression = DesiredCompression.Uncompressed;

            bspFile.SaveToFile(
                options.OutputPath ?? null,
                new BspFile.SaveToFileOptions
                {
                    Compression = compression,
                    Handler = null,
                    MakeBackup = !options.SkipBackup,
                    RenameCubemaps = options.RenameCubemaps,
                }
            );
        }

        if (options.JsonDump || options.JsonPath is not null)
            JsonDump(bspFile, options);

        return true;
    }

    private static void JsonDump(BspFile bspFile, CommandLineOptions options) =>
        bspFile.JsonDump(
            options.JsonPath ?? null,
            null,
            sortLumps: options.JsonOptions.HasFlag(JsonOptions.SortLumps),
            sortProperties: options.JsonOptions.HasFlag(JsonOptions.SortProperties),
            ignoreOffset: options.JsonOptions.HasFlag(JsonOptions.IgnoreOffset)
        );

    private static void RunWorkflow(BspFile bspFile, string workflowPath)
    {
        if (!File.Exists(workflowPath))
        {
            Logger.Warn("Could not find workflow file");
            return;
        }

        using FileStream stream = File.OpenRead(workflowPath);
        if (!Job.TryLoadWorkflow(stream, out List<Job>? workflow))
            return; // TryLoadWorkflow logs any exceptions

        Logger.Info("Loaded workflow file");
        foreach (Job job in workflow)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Logger.Info($"Running job: {job.JobNameInternal}");

            if (job.Run(bspFile))
                _shouldWriteOutput = true;

            stopwatch.Stop();
            Logger.Info($"Job completed in {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    private static bool CheckAssets(BspFile bspFile)
    {
        // Not bother with OriginFilter stuff, seems very unlikely to be helpful in Momentum's case.
        PakfileLump pakfileLump = bspFile.GetLump<PakfileLump>();

        int numMatches = 0;

        var sw = new Stopwatch();
        sw.Start();

        Logger.Info("Checking packed official assets (may take a bit...)");

        // Not parallelizing as zip reads block per-entry
        foreach (PakfileEntry entry in pakfileLump.Entries)
        {
            if (!AssetManifest.Manifest.TryGetValue(entry.Hash, out List<AssetManifest.Asset>? assets))
                continue;

            numMatches++;
            string matches = string.Join(
                ", ",
                assets.Select(asset => entry.Key == asset.Path ? asset.Origin : $"{asset.Origin} asset {asset.Path}")
            );
            Logger.Error($"{entry.Key} matched {matches}");
        }

        Logger.Debug($"Asset check completed in {sw.ElapsedMilliseconds}ms");

        if (numMatches > 0)
        {
            Logger.Error($"BSP file contains {numMatches} packed official game assets!");
            return false;
        }
        else
        {
            Logger.Info("Did not find any packed official game assets");
            return true;
        }
    }

    private static bool ValidateEntityRules(BspFile bspFile, string? rulesPath)
    {
        Dictionary<string, EntityRule> rules;
        try
        {
            rules = EntityRule.LoadRules(rulesPath);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Could not load rules file {rulesPath}", ex);
        }

        EntityLump entLump = bspFile.GetLump<EntityLump>();

        int noClassname = 0;
        Dictionary<EntityRule.AllowLevel, HashSet<string>> counts = new()
        {
            [EntityRule.AllowLevel.Allow] = [],
            [EntityRule.AllowLevel.Warn] = [],
            [EntityRule.AllowLevel.Deny] = [],
            [EntityRule.AllowLevel.Unknown] = [],
        };

        foreach (Entity entity in entLump.Data)
        {
            string? className = entity.Properties.FirstOrDefault(x => x.Key == "classname")?.ValueString;

            if (className is null)
                noClassname++;
            else if (!rules.TryGetValue(className, out EntityRule? rule))
                counts[EntityRule.AllowLevel.Unknown].Add(className);
            else
                counts[rule.Level].Add(className);
        }

        bool bad = false;
        Logger.Info("Entity rule validation results:");

        Logger.Info($"Allowed - {counts[EntityRule.AllowLevel.Allow].Count}");

        HashSet<string> warns = counts[EntityRule.AllowLevel.Warn];
        if (warns.Count > 0)
            Logger.Warn($"Warning - {warns.Count}\n{string.Join(", ", warns)}");
        else
            Logger.Info("Warning - 0");

        HashSet<string> disallows = counts[EntityRule.AllowLevel.Deny];
        if (disallows.Count > 0)
        {
            bad = true;
            Logger.Error($"Disallowed - {disallows.Count}\n{string.Join(", ", disallows)}");
        }
        else
        {
            Logger.Info("Disallowed - 0");
        }

        if (noClassname > 0)
        {
            bad = true;
            Logger.Error($"{noClassname} entities with no classname!!");
        }

        return !bad;
    }

    private static int ShowHelp(ParserResult<CommandLineOptions> parserResult)
    {
        // ReSharper disable PossibleMultipleEnumeration
        IEnumerable<Error>? errors = parserResult.Errors;
        string version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown Version";
        if (errors.IsVersion())
        {
            Logger.Info($"Lumper CLI v{version}");
            return 0;
        }

        Logger.Info(
            HelpText.AutoBuild(
                parserResult,
                h =>
                {
                    h.Heading = $"Lumper CLI v{version} [https://github.com/momentum-mod/lumper]";
                    h.AddNewLineBetweenHelpSections = false;
                    h.AdditionalNewLineAfterOption = false;
                    h.Copyright = "";
                    h.MaximumDisplayWidth = 100;
                    // Custom comparator to put values above options - by default options are first
                    // which I hate!!!
                    h.OptionComparison = (a, b) =>
                        a.IsOption switch
                        {
                            true when b.IsOption => a.Required switch
                            {
                                true when !b.Required => -1,
                                false when b.Required => 1,
                                _ => 0,
                            },
                            true when b.IsValue => 1,
                            _ => -1,
                        };
                    h.AddPostOptionsLines(
                        [
                            "Without options, Lumper will simply read the BSP file and exit. "
                                + "If any options provided cause modifications, the BSP will be output to the same path. "
                                + "If you want to output to a different path, use the -o option.",
                            "",
                            "If -c or -C are given, an output file will always be produced, even if unmodified. "
                                + "If neither -c or -C are given and a BSP is output, BSP will be written in its current state.",
                            "",
                            "Any parameters marked with [VALIDATOR] will cause the program to exit with status 0 if all "
                                + "validations pass, or 1 if any fail. Non-validator params will be ignored!",
                        ]
                    );
                    return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                },
                e => e
            )
        );

        return errors.IsHelp() ? 0 : 1;
    }
}
