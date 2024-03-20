namespace Lumper.Lib.BSP;
using System;
using System.Collections.Generic;
using System.Linq;
using Lumper.Lib.BSP.IO;
using Lumper.Lib.BSP.Lumps;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class JsonContractResolver(bool sortProperties, bool ignoreOffset) : DefaultContractResolver
{
    public bool SortProperties { get; } = sortProperties;
    public bool IgnoreOffset { get; } = ignoreOffset;

    protected override IList<JsonProperty> CreateProperties(Type type,
        MemberSerialization memberSerialization)
    {
        IList<JsonProperty> @base = base.CreateProperties(type, memberSerialization);
        IEnumerable<JsonProperty> ordered = @base;
        if (SortProperties)
        {
            ordered = ordered.OrderBy(p => p.Order ?? int.MaxValue)
                             .ThenBy(p => p.PropertyName);
        }

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
