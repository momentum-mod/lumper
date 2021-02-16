using System;
using System.IO;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class GenericLump : Lump
    {
        public override int DataSize => 0;
        public override void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }
    }
}