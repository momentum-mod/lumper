namespace Lumper.Lib.EntityRules;

using System;
using System.IO;
using Newtonsoft.Json;
using NLog;
using EntityRules = System.Collections.Generic.Dictionary<string, EntityRule>;

public class EntityRule
{
    public enum AllowLevel
    {
        // Ordered in order DataGrid list sorts by
        Deny = 0,
        Unknown = 1, // Should never be used in the JSON file!
        Warn = 2,
        Allow = 3,
    }

    public AllowLevel Level { get; set; }

    public string? Comment { get; set; }

    public static string DefaultRulesPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "resources", "EntityRules_Momentum.json");

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static EntityRules LoadRules(string? path)
    {
        path ??= DefaultRulesPath;

        if (!File.Exists(path))
            throw new ArgumentException($"Bad rule file path {path}");

        string rulesString = File.ReadAllText(path);

        EntityRules? rules = JsonConvert.DeserializeObject<EntityRules>(rulesString);
        if (rules is null)
        {
            Logger.Error($"Failed to parse rules file! Path: {path}");
            return [];
        }

        return rules;
    }
}
