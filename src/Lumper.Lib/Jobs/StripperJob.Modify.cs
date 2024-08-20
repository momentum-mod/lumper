namespace Lumper.Lib.Jobs;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using KvPair = System.Collections.Generic.KeyValuePair<string, string>;

public partial class StripperJob
{
    protected class Modify : Block
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
                    line = line.Trim();

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
                        (lineParam, lNrParam) => props.Add(ParseProp(lineParam, lNrParam))
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
                            $"Set value of {prop.Key} to {prop.ValueString} on entity {entity.PresentableName}"
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
