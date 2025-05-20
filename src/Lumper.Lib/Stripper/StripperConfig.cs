namespace Lumper.Lib.Stripper;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using NLog;
using KvPair = System.Collections.Generic.KeyValuePair<string, string>;

public partial class StripperConfig
{
    private StripperConfig() { }

    public List<Block> Blocks { get; } = [];

    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    // Throws if Parse fails, good for jobs
    public static StripperConfig Parse(Stream stream)
    {
        var config = new StripperConfig();

        var reader = new StreamReader(stream);
        int lineNr = 0;
        string prevBlock = "";
        while (reader.ReadLine() is { } line)
        {
            lineNr++;

            line = line.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(line))
                continue;

            bool blockOpen = false;
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
                "filter:" or "remove:" => new FilterBlock(),
                "add:" => new AddBlock(),
                "modify:" => new ModifyBlock(),
                _ => throw new NotImplementedException($"Unknown block '{line}' in line {lineNr}"),
            };
            prevBlock = line;

            block.Parse(reader, blockOpen, ref lineNr);
            config.Blocks.Add(block);
        }

        return config;
    }

    public static bool TryParse(
        Stream stream,
        [NotNullWhen(true)] out StripperConfig? config,
        [NotNullWhen(false)] out string? errorMessage
    )
    {
        try
        {
            config = Parse(stream);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            config = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    [GeneratedRegex("\"([^\"]+)\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex PairRegex();

    private static bool IsComment(string line)
    {
        return line.StartsWith(';')
            || line.StartsWith("//", StringComparison.Ordinal)
            || line.StartsWith('#')
            || line == "";
    }

    private static bool MatchKeyValue(KvPair filter, Entity.EntityProperty property)
    {
        if (!filter.Key.Equals(property.Key, StringComparison.OrdinalIgnoreCase))
            return false;

        // Match non-regex
        if (!(filter.Value.Length > 2 && filter.Value.StartsWith('/') && filter.Value.EndsWith('/')))
            return filter.Value.Equals(property.ValueString, StringComparison.OrdinalIgnoreCase);

        // Match regex
        try
        {
            var regex = new Regex(filter.Value[1..^2], RegexOptions.IgnoreCase);
            return regex.IsMatch(property.ValueString);
        }
        catch (Exception _) when (_ is ArgumentException or ArgumentNullException)
        {
            Logger.Warn(
                $"Error: Invalid regex {filter.Value}. Stripper uses Perl-style regexes, Lumper uses .NET; your regex may need adjusting."
            );
            return false;
        }
    }

    public abstract class Block
    {
        public abstract void Parse(StreamReader reader, bool blockOpen, ref int lineNr);

        public abstract void Apply(EntityLump lump);

        protected static KvPair ParseProperty(string line, int lineNr)
        {
            Match match = PairRegex().Match(line);
            if (!match.Success)
                throw new InvalidDataException($"Can't parse KeyValuePair '{line}' in line {lineNr}");

            return new KvPair(match.Groups[1].Value, match.Groups[2].Value);
        }

        protected static void ParseBlock(StreamReader reader, bool blockOpen, ref int lineNr, Action<string, int> fn)
        {
            while (reader.ReadLine() is { } line)
            {
                lineNr++;
                line = line.Trim();

                if (string.IsNullOrEmpty(line))
                    continue;

                if (!blockOpen && line == "{")
                {
                    blockOpen = true;
                }
                else if (blockOpen)
                {
                    if (line == "}")
                        break;

                    fn(line, lineNr);
                }
                else
                {
                    throw new InvalidDataException($"Can't get KeyValuePair from '{line}' line {lineNr}");
                }
            }
        }
    }

    public class AddBlock : Block
    {
        public List<KvPair> Properties { get; set; } = [];

        public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr)
        {
            ParseBlock(reader, blockOpen, ref lineNr, (line, lNr) => Properties.Add(ParseProperty(line, lNr)));
        }

        public override void Apply(EntityLump lump)
        {
            Entity entity = new(Properties);
            lump.Data.Add(entity);
            Logger.Info($"Created entity {entity.PresentableName}");
        }
    }

    public class FilterBlock : Block
    {
        public List<KvPair> Properties { get; set; } = [];

        public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr)
        {
            ParseBlock(reader, blockOpen, ref lineNr, (line, lNr) => Properties.Add(ParseProperty(line, lNr)));
        }

        public override void Apply(EntityLump lump)
        {
            foreach (
                Entity entity in lump.Data.Where(entity =>
                    Properties.All(filterProperty =>
                        entity.Properties.Any(entityProperty => MatchKeyValue(filterProperty, entityProperty))
                    )
                )
            )
            {
                lump.Data.Remove(entity);
                Logger.Info($"Removed entity {entity.PresentableName}");
            }
        }
    }

    public class ModifyBlock : Block
    {
        private List<KvPair> Match { get; } = [];
        private List<KvPair> Replace { get; } = [];
        private List<KvPair> Delete { get; } = [];
        private List<KvPair> Insert { get; } = [];

        public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr)
        {
            string prevBlock = "";
            ParseBlock(
                reader,
                blockOpen,
                ref lineNr,
                (line, lNr) =>
                {
                    line = line.Trim().ToLowerInvariant();

                    bool blockOpenInner = false;
                    if (line == "{")
                    {
                        line = prevBlock;
                        blockOpenInner = true;
                    }
                    else if (IsComment(line))
                    {
                        return;
                    }

                    List<KvPair> props = line switch
                    {
                        "match:" => Match,
                        "replace:" => Replace,
                        "delete:" => Delete,
                        "insert:" => Insert,
                        _ => throw new InvalidDataException($"Unknown title {line} in line {lNr}"),
                    };

                    prevBlock = line;

                    ParseBlock(
                        reader,
                        blockOpenInner,
                        ref lNr,
                        (lineParam, lNrParam) => props.Add(ParseProperty(lineParam, lNrParam))
                    );
                }
            );
        }

        public override void Apply(EntityLump lump)
        {
            foreach (
                Entity entity in lump
                    .Data.Where(entity =>
                        Match.All(filterProperty =>
                            entity.Properties.Any(entityProperty => MatchKeyValue(filterProperty, entityProperty))
                        )
                    )
                    .ToList()
            )
            {
                foreach (KvPair replaceProp in Replace)
                {
                    foreach (
                        Entity.EntityProperty prop in entity
                            .Properties.Where(prop => prop.Key == replaceProp.Key)
                            .ToList()
                    )
                    {
                        var newProp = Entity.EntityProperty.Create(replaceProp.Key, replaceProp.Value);
                        if (newProp is null)
                            continue;

                        entity.Properties[entity.Properties.IndexOf(prop)] = newProp;
                        Logger.Info(
                            $"Set value of {prop.Key} to {newProp.ValueString} on entity {entity.PresentableName}"
                        );
                    }
                }

                foreach (
                    Entity.EntityProperty toDelete in Delete.SelectMany(deleteProp =>
                        entity.Properties.Where(prop => MatchKeyValue(deleteProp, prop)).ToList()
                    )
                )
                {
                    entity.Properties.Remove(toDelete);
                    Logger.Info($"Removed property {toDelete} on entity {entity.PresentableName}");
                }

                foreach (KvPair insertProp in Insert)
                {
                    var newProp = Entity.EntityProperty.Create(insertProp);
                    if (newProp is null)
                        return;

                    entity.Properties.Add(newProp);
                    Logger.Info(
                        $"Added property {newProp.Key}: {newProp.ValueString} to entity {entity.PresentableName}"
                    );
                }
            }
        }
    }
}
