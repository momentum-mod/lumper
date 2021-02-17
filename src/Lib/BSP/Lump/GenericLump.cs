using System;
using System.IO;

namespace MomBspTools.Lib.BSP.Lump
{
    public class GenericLump : AbstractLump
    {
        public override int DataSize => 0;
        public override void Read(BinaryReader r)
        {
            throw new NotImplementedException();
        }

        public GenericLump(BspFile parent) : base(parent)
        {
        }
    }
}