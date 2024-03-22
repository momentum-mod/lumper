namespace Lumper.Lib.Tasks;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;
using Prop = System.Collections.Generic.KeyValuePair<string, string>;

public partial class StripperTask
{
    protected abstract partial class Block
    {
        private static readonly Regex pairRegex =
            MyRegex();

        public abstract void Parse(StreamReader reader, bool blockOpen, ref int lineNr);

        protected static Prop ParseProp(string line, int lineNr)
        {
            Match match = pairRegex.Match(line);
            if (match.Success)
            {
                var pair = new Prop(
                    match.Groups[1].Value,
                    match.Groups[2].Value);
                return pair;
            }
            else
            {
                throw new NotImplementedException($"Can't parse KeyValuePair '{line}' in line {lineNr}");
            }
        }
        protected static void ParseBlock(StreamReader reader, bool blockOpen, ref int lineNr, Action<string, int> fun)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNr++;
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                else if (!blockOpen && line == "{")
                {
                    blockOpen = true;
                }
                else if (blockOpen)
                {
                    if (line == "}")
                        break;
                    else
                        fun(line, lineNr);
                }
                else
                {
                    throw new NotImplementedException($"Can't get KeyValuePair from '{line}' line {lineNr}");
                }
            }
        }

        public abstract void Apply(EntityLump lump);
        protected static bool MatchKeyValue(Prop filterProp, Entity.Property entityProp)
        {
            if (filterProp.Key != entityProp.Key)
                return false;

            if (filterProp.Value.Length > 2 && filterProp.Value.StartsWith('/') && filterProp.Value.EndsWith('/') &&
                entityProp.ValueString is not null)
            {
                try
                {
                    var regex = new Regex(filterProp.Value[1..^2]);
                    return regex.IsMatch(entityProp.ValueString);
                }
                catch (Exception _) when (_ is ArgumentException or ArgumentNullException)
                {
                    Logger.Warn(
                        $"Error: Invalid regex {filterProp.Value}. Stripper uses Perl-style regexes, Lumper uses .NET; your regex may need adjusting.");
                }
            }
            return filterProp.Value == entityProp.ValueString;
        }

        [GeneratedRegex("\"([^\"]+)\"\\s+\"([^\"]+)\"")]
        private static partial Regex MyRegex();
    }
}
