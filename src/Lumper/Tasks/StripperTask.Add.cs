namespace Lumper.Lib.Tasks;
using System.Collections.Generic;
using System.IO;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;

using Prop = KeyValuePair<string, string>;

public partial class StripperTask
{
    protected class Add : Block
    {
        public List<Prop> Properties { get; set; } = [];

        public Add()
        { }

        public override void Parse(StreamReader reader, bool blockOpen, ref int lineNr) => ParseBlock(reader, blockOpen, ref lineNr, (line, lNr) => Properties.Add(ParseProp(line, lNr)));

        public override void Apply(EntityLump lump) => lump.Data.Add(new Entity(Properties));
    }
}
