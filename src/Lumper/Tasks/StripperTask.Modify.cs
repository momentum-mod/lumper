using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;

namespace Lumper.Lib.Tasks
{
    using Prop = KeyValuePair<string, string>;
    public partial class StripperTask
    {
        protected class Modify : Block
        {
            public List<Prop> Match { get; set; } = new();
            public List<Prop> Replace { get; set; } = new();
            public List<Prop> Delete { get; set; } = new();
            public List<Prop> Insert { get; set; } = new();

            public Modify()
            { }

            public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr)
            {
                string prevBlock = "";
                ParseBlock(reader, blockOpen, ref lineNr, (line, lNr) =>
                {
                    line = line.Trim();
                    List<Prop> props = null;

                    bool blockOpen = false;
                    if (line == "{")
                    {
                        line = prevBlock;
                        blockOpen = true;
                    }
                    else if (IsComment(line))
                        return;

                    switch (line)
                    {
                        case "match:":
                            props = Match;
                            break;
                        case "replace:":
                            props = Replace;
                            break;
                        case "delete:":
                            props = Delete;
                            break;
                        case "insert:":
                            props = Insert;
                            break;
                        default:
                            throw new NotImplementedException(
                                $"Unknown title {line} in line {lNr}");
                    }
                    prevBlock = line;

                    ParseBlock(reader, blockOpen, ref lNr, (line, lNr) =>
                        {
                            props.Add(ParseProp(line, lNr));
                        });
                });
            }

            public override void Apply(EntityLump lump)
            {
                foreach (var entity in lump.Data)
                {
                    if (Match.All(
                        f => entity.Properties.Any(
                            e => MatchKeyValue(f, e))))
                    {
                        foreach (var prop in Replace)
                        {
                            var replaceEntProp = entity.Properties.Where(
                                x => x.Key == prop.Key);
                            foreach (var rep in replaceEntProp)
                            {
                                rep.ValueString = prop.Value;
                            }
                        }
                        foreach (var prop in Delete)
                        {
                            var deleteEntProp = new List<Entity.Property>();
                            foreach (var del in entity.Properties)
                            {
                                if (MatchKeyValue(prop, del))
                                    deleteEntProp.Add(del);
                            }
                            foreach (var del in deleteEntProp)
                                entity.Properties.Remove(del);
                        }
                        foreach (var prop in Insert)
                        {
                            entity.Properties.Add(Entity.Property.CreateProperty(prop));
                        }
                    }
                }
            }
        }
    }
}