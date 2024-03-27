namespace Lumper.Lib.Tasks;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;
using NLog;
using Prop = System.Collections.Generic.KeyValuePair<string, string>;

public partial class StripperTask
{
    protected abstract partial class Block
    {
        [GeneratedRegex("\"([^\"]+)\"\\s+\"([^\"]+)\"")]
        private static partial Regex PairRegex();

        public abstract void Parse(StreamReader reader, bool blockOpen, ref int lineNr);

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected static Prop ParseProp(string line, int lineNr)
        {
            Match match = PairRegex().Match(line);
            if (match.Success)
            {
                var pair = new Prop(
                    match.Groups[1].Value,
                    match.Groups[2].Value);
                return pair;
            }
            else
            {
                throw new InvalidDataException($"Can't parse KeyValuePair '{line}' in line {lineNr}");
            }
        }

        protected static void ParseBlock(StreamReader reader, bool blockOpen, ref int lineNr, Action<string, int> fun)
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
                    {
                        break;
                    }

                    fun(line, lineNr);
                }
                else
                {
                    Logger.Error($"Can't get KeyValuePair from '{line}' line {lineNr}");
                }
            }
        }

        public abstract void Apply(EntityLump lump);

        protected static bool MatchKeyValue(Prop filterProp, Entity.EntityProperty entityProp)
        {
            (var filterKey, var filterValue) = filterProp;

            if (filterKey != entityProp.Key)
                return false;

            if (filterValue.Length > 2 && filterValue.StartsWith('/') && filterValue.EndsWith('/'))
            {
                try
                {
                    var regex = new Regex(filterValue[1..^2]);
                    return regex.IsMatch(entityProp.ValueString);
                }
                catch (Exception _) when (_ is ArgumentException or ArgumentNullException)
                {
                    Console.WriteLine($"Error: Invalid regex {filterValue}. Stripper uses Perl-style regexes, Lumper uses .NET; your regex may need adjusting.");
                }
            }

            return filterProp.Value == entityProp.ValueString;
        }
    }
}
