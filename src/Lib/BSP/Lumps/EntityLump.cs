using System;
using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class EntityLump : Lump
    {
        // TODO
        public override int DataSize { get; }
        public override void Read(BinaryReader r)
        {
            throw new NotImplementedException();
        }

        public EntityLump(BspFile parent) : base(parent)
        {
        }
    }
}