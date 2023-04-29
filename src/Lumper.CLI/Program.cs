using CommandLine;
using Lumper.Lib.BSP;

namespace Lumper.CLI;

internal class Program
{
    public static void Main(string[] args)
    {
        var parser = new Parser(with =>
        {
            with.HelpWriter = Console.Out;
            with.CaseInsensitiveEnumValues = true;
        });
        var parserResult = parser.ParseArguments<CommandLineOptions>(args);

        string? path = null;
        parserResult
            .WithParsed(x => path = x.Path)
            .WithNotParsed(x => CommandLineOptions.ErrorHandler(x));
        if (path == null)
            return;

        if (parserResult.Value.Json.Any())
        {
            var bspFile = new BspFile(path);

            bool sortLumps = parserResult.Value.Json.Any(
                x => x == JsonOptions.SortLumps);
            bool sortProperties = parserResult.Value.Json.Any(
                x => x == JsonOptions.SortProperties);
            bool ignoreOffset = parserResult.Value.Json.Any(
                x => x == JsonOptions.IgnoreOffset);
            bspFile.ToJson(sortLumps, sortProperties, ignoreOffset);
        }
    }
}