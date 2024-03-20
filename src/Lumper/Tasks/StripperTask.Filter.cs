namespace Lumper.Lib.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;

using Prop = KeyValuePair<string, string>;

public partial class StripperTask
{
    protected class Filter : Block
    {
        public List<Prop> Properties { get; set; } = [];

        public Filter()
        { }

        public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr) => ParseBlock(reader, blockOpen, ref lineNr, (line, lNr) => Properties.Add(ParseProp(line, lNr)));

        public override void Apply(EntityLump lump)
        {
            var del = new List<Entity>();

            foreach (Entity entity in lump.Data)
            {
                if (Properties.All(
                    f => entity.Properties.Any(
                        e => MatchKeyValue(f, e))))
                {
                    del.Add(entity);
                }
            }

            foreach (Entity entity in del)
            {
                lump.Data.Remove(entity);
            }
        }
    }
}
