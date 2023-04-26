using System;
using System.Linq;
using System.Collections.Generic;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Lumper.Lib.BSP
{
    public class JsonContractResolver : DefaultContractResolver
    {
        public bool SortProperties { get; }
        public bool IgnoreOffset { get; }

        public JsonContractResolver(bool sortProperties, bool ignoreOffset)
        {
            SortProperties = sortProperties;
            IgnoreOffset = ignoreOffset;
        }

        protected override IList<JsonProperty> CreateProperties(Type type,
            MemberSerialization memberSerialization)
        {
            var @base = base.CreateProperties(type, memberSerialization);
            IEnumerable<JsonProperty> ordered = @base;
            if (SortProperties)
                ordered = ordered.OrderBy(p => p.Order ?? int.MaxValue)
                                 .ThenBy(p => p.PropertyName);
            if (IgnoreOffset)
            {
                ordered = ordered.Where(x =>
                    !(x.DeclaringType.IsAssignableTo(typeof(IUnmanagedLump))
                    && x.PropertyName == nameof(IUnmanagedLump.DataStreamOffset)));
                ordered = ordered.Where(x =>
                    !((
                       x.DeclaringType.IsAssignableFrom(typeof(LumpHeader))
                    || x.DeclaringType.IsAssignableFrom(typeof(BspLumpHeader))
                    || x.DeclaringType.IsAssignableFrom(typeof(GameLumpHeader))
                    )
                    && x.PropertyName == nameof(LumpHeader.Offset)));
            }
            return ordered.ToList();
        }
    }
}