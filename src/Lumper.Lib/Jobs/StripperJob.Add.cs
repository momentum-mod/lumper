namespace Lumper.Lib.Jobs;

using System.Collections.Generic;
using System.IO;
using BSP.Lumps.BspLumps;
using BSP.Struct;
using Prop = System.Collections.Generic.KeyValuePair<string, string>;

public partial class StripperJob
{
    protected class Add : Block
    {
        public List<Prop> Properties { get; set; } = [];

        public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr) =>
            ParseBlock(reader, blockOpen, ref lineNr, (line, lNr) => Properties.Add(ParseProp(line, lNr)));

        public override void Apply(EntityLump lump)
        {
            Entity entity = new(Properties);
            lump.Data.Add(entity);
            Logger.Info($"Created entity {entity.PresentableName}");
        }
    }
}
