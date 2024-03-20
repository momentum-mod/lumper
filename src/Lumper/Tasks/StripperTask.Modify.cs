namespace Lumper.Lib.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;
using Prop = System.Collections.Generic.KeyValuePair<string, string>;

public partial class StripperTask
{
    protected class Modify : Block
    {
        public List<Prop> Match { get; set; } = [];
        public List<Prop> Replace { get; set; } = [];
        public List<Prop> Delete { get; set; } = [];
        public List<Prop> Insert { get; set; } = [];

        public Modify()
        { }

        public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr)
        {
            var prevBlock = "";
            ParseBlock(reader, blockOpen, ref lineNr, (line, lNr) =>
            {
                line = line.Trim();
                List<Prop> props = null;

                var blockOpen = false;
                if (line == "{")
                {
                    line = prevBlock;
                    blockOpen = true;
                }
                else if (IsComment(line))
                {
                    return;
                }

                props = line switch
                {
                    "match:" => Match,
                    "replace:" => Replace,
                    "delete:" => Delete,
                    "insert:" => Insert,
                    _ => throw new NotImplementedException($"Unknown title {line} in line {lNr}"),
                };
                prevBlock = line;

                ParseBlock(reader, blockOpen, ref lNr, (line, lNr) => props.Add(ParseProp(line, lNr)));
            });
        }

        public override void Apply(EntityLump lump)
        {
            foreach (Entity entity in lump.Data)
            {
                if (Match.All(
                    f => entity.Properties.Any(
                        e => MatchKeyValue(f, e))))
                {
                    foreach (Prop prop in Replace)
                    {
                        IEnumerable<Entity.Property> replaceEntProp = entity.Properties.Where(
                            x => x.Key == prop.Key);
                        foreach (Entity.Property? rep in replaceEntProp)
                        {
                            rep.ValueString = prop.Value;
                        }
                    }
                    foreach (Prop prop in Delete)
                    {
                        var deleteEntProp = new List<Entity.Property>();
                        foreach (Entity.Property del in entity.Properties)
                        {
                            if (MatchKeyValue(prop, del))
                                deleteEntProp.Add(del);
                        }
                        foreach (Entity.Property del in deleteEntProp)
                            entity.Properties.Remove(del);
                    }
                    foreach (Prop prop in Insert)
                    {
                        entity.Properties.Add(Entity.Property.CreateProperty(prop));
                    }
                }
            }
        }
    }
}
