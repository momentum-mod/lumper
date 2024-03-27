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
        IList<JsonProperty> baseProperties = base.CreateProperties(type, memberSerialization);
        IEnumerable<JsonProperty> orderedProperties = baseProperties;
        if (SortProperties)
        {
            orderedProperties = orderedProperties.OrderBy(p => p.Order ?? int.MaxValue)
                .ThenBy(p => p.PropertyName);
        }

        if (IgnoreOffset)
        {
            orderedProperties = orderedProperties.Where(
                prop => prop.DeclaringType is not null &&
                        !(prop.DeclaringType.IsAssignableTo(typeof(IUnmanagedLump)) &&
                          prop.PropertyName == nameof(IUnmanagedLump.DataStreamOffset)) &&
                        !((
                            prop.DeclaringType.IsAssignableFrom(typeof(LumpHeader))
                            || prop.DeclaringType.IsAssignableFrom(typeof(BspLumpHeader))
                            || prop.DeclaringType.IsAssignableFrom(typeof(GameLumpHeader))
                        ) && prop.PropertyName == nameof(LumpHeader.Offset))
                );
        }

        return orderedProperties.ToList();
    }
}
