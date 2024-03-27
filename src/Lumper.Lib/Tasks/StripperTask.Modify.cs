namespace Lumper.Lib.Tasks;
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

                var blockOpenInner = false;
                if (line == "{")
                {
                    line = prevBlock;
                    blockOpenInner = true;
                }
                else if (IsComment(line))
                {
                    return;
                }

                List<Prop> props = line switch
                {
                    "match:" => Match,
                    "replace:" => Replace,
                    "delete:" => Delete,
                    "insert:" => Insert,
                    _ => throw new InvalidDataException($"Unknown title {line} in line {lNr}"),
                };

                prevBlock = line;

                ParseBlock(reader, blockOpenInner, ref lNr, (lineParam, lNrParam) => props.Add(ParseProp(lineParam, lNrParam)));
            });
        }

        public override void Apply(EntityLump lump)
        {
            foreach (Entity entity in lump.Data.Where(entity => Match.All(
                filterProperty => entity.Properties.Any(
                    entityProperty => MatchKeyValue(filterProperty, entityProperty)))))
            {
                foreach (Prop replaceProp in Replace)
                {
                    foreach (Entity.EntityProperty prop in
                        entity.Properties.Where(prop => prop.Key == replaceProp.Key))
                    {
                        prop.ValueString = replaceProp.Value;
                    }
                }

                foreach (Prop deleteProp in Delete)
                {
                    foreach (Entity.EntityProperty toDelete in
                        entity.Properties.Where(prop => MatchKeyValue(deleteProp, prop)))
                    {
                        entity.Properties.Remove(toDelete);
                    }
                }

                foreach (Prop insertProp in Insert)
                {
                    var newProp = Entity.EntityProperty.CreateProperty(insertProp);
                    if (newProp is not null)
                        entity.Properties.Add(newProp);
                }
            }
        }
    }
}
