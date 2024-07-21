namespace Lumper.Lib.BSP;

using System;
using System.Collections.Generic;
using System.Linq;
using IO;
using Lumps;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class JsonContractResolver(bool sortProperties, bool ignoreOffset) : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type,
        MemberSerialization memberSerialization)
    {
        IList<JsonProperty> baseProperties = base.CreateProperties(type, memberSerialization);

        IEnumerable<JsonProperty> orderedProperties = baseProperties;

        if (sortProperties)
        {
            orderedProperties = orderedProperties
                .OrderBy(p => p.Order ?? int.MaxValue)
                .ThenBy(p => p.PropertyName);
        }

        if (ignoreOffset)
        {
            orderedProperties = orderedProperties.Where(
                prop => prop.DeclaringType is not null &&
                        !(prop.DeclaringType.IsAssignableTo(typeof(IUnmanagedLump)) &&
                          prop.PropertyName == nameof(IUnmanagedLump.DataStreamOffset)) &&
                        !((prop.DeclaringType.IsAssignableFrom(typeof(LumpHeaderInfo)) ||
                           prop.DeclaringType.IsAssignableFrom(typeof(BspLumpHeader)) ||
                           prop.DeclaringType.IsAssignableFrom(typeof(GameLumpHeader))) &&
                          prop.PropertyName == nameof(LumpHeaderInfo.Offset))
            );
        }

        return orderedProperties.ToList();
    }
}
