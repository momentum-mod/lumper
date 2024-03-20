using CommandLine;

public enum JsonOptions
{
    Default,
    SortLumps,
    SortProperties,
    IgnoreOffset,
}

public class CommandLineOptions
{
    [Value(index: 0,
           Required = false,
           HelpText = "BSP file path")]
    public string? Path { get; set; }

    private const string jsonOptionName = "json";
    [Option(jsonOptionName,
            Separator = ',',
            //required because its the only thing the cli can do for now
            Required = true,
            HelpText = "Export to JSON (for comparison)")]
    public IEnumerable<JsonOptions>? Json { get; set; }
    public static void ErrorHandler(IEnumerable<Error> errors)
    {
        if (errors.Any(
            x => (x is BadFormatConversionError or MissingValueOptionError)
                 && ((NamedError)x).NameInfo.LongName == jsonOptionName))
        {
            Console.WriteLine("Available JSON options: " +
                string.Join(", ", Enum.GetNames(typeof(JsonOptions))));
        }
    }
}
