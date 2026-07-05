namespace Lumper.Lib.FGD;

using System;
using System.Collections.Generic;

public record FGDInput(string Name, string ParameterType, string Description);

public record FGDOutput(string Name, string ParameterType, string Description);

public record FGDProperty(
    string Name, 
    string ValueType, 
    string DisplayName, 
    string DefaultValue, 
    string Description, 
    IReadOnlyDictionary<string, string> Choices
);

public record FGDEntity(
    string ClassName,
    string ClassType,
    string Description,
    IReadOnlyDictionary<string, FGDProperty> Properties,
    IReadOnlyDictionary<string, FGDInput> Inputs,
    IReadOnlyDictionary<string, FGDOutput> Outputs
);

public record FGDEntityRaw(
    string ClassName,
    string ClassType,
    string Description,
    IReadOnlyList<string> Bases,
    IReadOnlyDictionary<string, FGDProperty> Properties,
    IReadOnlyDictionary<string, FGDInput> Inputs,
    IReadOnlyDictionary<string, FGDOutput> Outputs
);

public static class FGD
{
    public static IReadOnlyDictionary<string, FGDEntity> ResolveInheritance(IReadOnlyDictionary<string, FGDEntityRaw> rawEntities)
    {
        var cache = new Dictionary<string, FGDEntity>(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, FGDEntity>();

        foreach (FGDEntityRaw ent in rawEntities.Values)
        {
            if (string.Equals(ent.ClassType, "BaseClass", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result[ent.ClassName] = ResolveOne(ent, rawEntities, cache, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        return result;
    }

    private static FGDEntity ResolveOne(
        FGDEntityRaw ent,
        IReadOnlyDictionary<string, FGDEntityRaw> rawEntities,
        Dictionary<string, FGDEntity> cache,
        HashSet<string> visiting)
    {
        if (cache.TryGetValue(ent.ClassName, out FGDEntity? cached))
        {
            return cached;
        }

        if (!visiting.Add(ent.ClassName))
        {
            // Handle circular base reference
            return new FGDEntity(
                ent.ClassName,
                ent.ClassType,
                ent.Description,
                new Dictionary<string, FGDProperty>(),
                new Dictionary<string, FGDInput>(),
                new Dictionary<string, FGDOutput>());
        }

        var properties = new Dictionary<string, FGDProperty>();
        var inputs = new Dictionary<string, FGDInput>();
        var outputs = new Dictionary<string, FGDOutput>();

        foreach (string baseName in ent.Bases)
        {
            // Skip bases not in the FGD
            if (!rawEntities.TryGetValue(baseName, out FGDEntityRaw? baseDecl))
            {
                continue;
            }

            FGDEntity baseDef = ResolveOne(baseDecl, rawEntities, cache, visiting);
            foreach (KeyValuePair<string, FGDProperty> kv in baseDef.Properties) properties[kv.Key] = kv.Value;
            foreach (KeyValuePair<string, FGDInput> kv in baseDef.Inputs) inputs[kv.Key] = kv.Value;
            foreach (KeyValuePair<string, FGDOutput> kv in baseDef.Outputs) outputs[kv.Key] = kv.Value;
        }

        foreach (KeyValuePair<string, FGDProperty> kv in ent.Properties) properties[kv.Key] = kv.Value;
        foreach (KeyValuePair<string, FGDInput> kv in ent.Inputs) inputs[kv.Key] = kv.Value;
        foreach (KeyValuePair<string, FGDOutput> kv in ent.Outputs) outputs[kv.Key] = kv.Value;

        var result = new FGDEntity(ent.ClassName, ent.ClassType, ent.Description, properties, inputs, outputs);
        cache[ent.ClassName] = result;
        visiting.Remove(ent.ClassName);
        return result;
    }
}