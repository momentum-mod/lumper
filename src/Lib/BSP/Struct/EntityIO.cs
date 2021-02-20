using System;
using System.Collections.Generic;
using System.Linq;

namespace MomBspTools.Lib.BSP.Struct
{
    public class EntityIO
    {
        public string TargetEntityName { get; set; }
        public string Input { get; set; }
        public string Parameter { get; set; }
        public float Delay { get; set; }
        public int TimesToFire { get; set; }

        public static bool IsIO(string value) => value.Count(s => s == ',') == 4;
    }
}