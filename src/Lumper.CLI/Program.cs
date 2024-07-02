namespace Lumper.CLI;

using CommandLine;
using Lib.BSP;
using Lib.BSP.IO;
using NLog;

internal sealed class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Main(string[] args)
    {
        var parser = new Parser(with =>
        {
            with.HelpWriter = Console.Out;
            with.CaseInsensitiveEnumValues = true;
        });
        ParserResult<CommandLineOptions> parserResult = parser.ParseArguments<CommandLineOptions>(args);

        string? path = null;
        parserResult
            .WithParsed(x => path = x.Path)
            .WithNotParsed(CommandLineOptions.ErrorHandler);
        if (path == null)
            return;

        if (parserResult.Value.Json is null || !parserResult.Value.Json.Any())
            return;

        var bspFile = BspFile.FromPath(new IoHandler(new CancellationTokenSource()), path);
        if (bspFile is null)
        {
            Logger.Error("Failed to load BSP file");
            return;
        }

        var sortLumps = parserResult.Value.Json.Any(x => x == JsonOptions.SortLumps);
        var sortProperties = parserResult.Value.Json.Any(x => x == JsonOptions.SortProperties);
        var ignoreOffset = parserResult.Value.Json.Any(x => x == JsonOptions.IgnoreOffset);
        bspFile.JsonDump(new IoHandler(new CancellationTokenSource()), sortLumps, sortProperties, ignoreOffset);
    }
}
