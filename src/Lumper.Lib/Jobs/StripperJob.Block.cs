namespace Lumper.Lib.Jobs;

using System;
using System.IO;
using System.Text.RegularExpressions;
using BSP.Lumps.BspLumps;
using BSP.Struct;
using Prop = System.Collections.Generic.KeyValuePair<string, string>;

public partial class StripperJob
{
    protected abstract partial class Block
    {
        [GeneratedRegex("\"([^\"]+)\"\\s+\"([^\"]+)\"")]
        private static partial Regex PairRegex();

        public abstract void Parse(StreamReader reader, bool blockOpen, ref int lineNr);

        protected static Prop ParseProp(string line, int lineNr)
        {
            Match match = PairRegex().Match(line);
            if (!match.Success)
                throw new InvalidDataException($"Can't parse KeyValuePair '{line}' in line {lineNr}");

            return new Prop(match.Groups[1].Value, match.Groups[2].Value);
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

        public abstract void Apply(EntityLump lump);

        protected static bool MatchKeyValue(Prop filterProp, Entity.EntityProperty entityProp)
        {
            if (filterProp.Key != entityProp.Key)
                return false;

            if (
                filterProp.Value.Length > 2
                && filterProp.Value.StartsWith('/')
                && filterProp.Value.EndsWith('/')
                && entityProp.ValueString is not null
            )
            {
                try
                {
                    var regex = new Regex(filterProp.Value[1..^2]);
                    return regex.IsMatch(entityProp.ValueString);
                }
                catch (Exception _) when (_ is ArgumentException or ArgumentNullException)
                {
                    Logger.Warn(
                        $"Error: Invalid regex {filterProp.Value}. Stripper uses Perl-style regexes, Lumper uses .NET; your regex may need adjusting."
                    );
                }
            }
            return filterProp.Value == entityProp.ValueString;
        }
    }
}
