using System;
using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class GenericLump : Lump
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