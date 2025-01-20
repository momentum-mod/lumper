namespace Lumper.Lib.Util;

using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using EntityRules = System.Collections.Generic.Dictionary<string, EntityRule>;

public class EntityRule
{
    public enum AllowLevel
    {
        Allow,
        Warn,
        Deny,
        Unknown, // Should never be used in the JSON file!
        NoClassname, // As above
    }

    public AllowLevel Level { get; set; }

    public string? Comment { get; set; }

    public static string DefaultRulesPath { get; } =
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "entityrules_momentum.json");

    public static EntityRules LoadRules(string? path)
    {
        path ??= DefaultRulesPath;

        if (!File.Exists(path))
            throw new ArgumentException($"Bad rule file path {path}");

        string rulesString = File.ReadAllText(path);

        return JsonConvert.DeserializeObject<EntityRules>(rulesString)
            ?? throw new ArgumentException($"Bad rule file {path}");
    }
}
