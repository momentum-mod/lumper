namespace Lumper.Lib.Jobs;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Prop = System.Collections.Generic.KeyValuePair<string, string>;

public partial class StripperJob
{
    protected class Filter : Block
    {
        public List<Prop> Properties { get; set; } = [];

        public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr) =>
            ParseBlock(reader, blockOpen, ref lineNr, (line, lNr) => Properties.Add(ParseProp(line, lNr)));

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
}
