namespace Lumper.Lib.Bsp.Struct;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using NLog;

public partial class Entity : ICloneable
{
    public Entity()
        : this(null) { }

    public int EntityLumpVersion { get; init; }

    public List<EntityProperty> Properties { get; set; }

    public Entity(IEnumerable<KeyValuePair<string, string>>? kv)
    {
        Properties = kv is not null
            ? kv.Select(x => EntityProperty.Create(x, EntityLumpVersion)).OfType<EntityProperty>().ToList()
            : [];
    }

    /// <summary>
    /// Provides a user-friendly name for the entity. classnames aren't unique and hammerids
    /// aren't always there, so can't consistently identify an entity without enumerating the
    /// entirety of its properties.
    /// </summary>
    public string PresentableName
    {
        get
        {
            string? hammerid = Properties
                .OfType<EntityProperty<string>>()
                .FirstOrDefault(x => x.Key == "hammerid")
                ?.Value;

            string className =
                Properties.OfType<EntityProperty<string>>().FirstOrDefault(x => x.Key == "classname")?.Value
                ?? "<missing classname>";

            return hammerid is not null ? $"{className} [HammerID {hammerid}]" : className;
        }
    }

    public static bool TryParsePosition(string value, [NotNullWhen(true)] out Vector3? vec)
    {
        vec = null;
        string[] parts = value.Split(' ');
        if (parts.Length != 3)
            return false;

        if (
            !float.TryParse(parts[0], CultureInfo.InvariantCulture, out float x)
            || !float.TryParse(parts[1], CultureInfo.InvariantCulture, out float y)
            || !float.TryParse(parts[2], CultureInfo.InvariantCulture, out float z)
        )
            return false;

        vec = new Vector3(x, y, z);
        return true;
    }

    [GeneratedRegex(@"^\*\d+$")]
    private static partial Regex BrushEntityRegex();

    public bool IsBrushEntity =>
        Properties.FirstOrDefault(x => x.Key == "model") is EntityProperty<string> { Value: string modelString }
        && BrushEntityRegex().IsMatch(modelString);

    public bool IsWithinSphere(Vector3 position, int radius)
    {
        if (
            Properties.FirstOrDefault(p => p.Key == "origin")
            is not EntityProperty<string> { ValueString: string originString }
        )
            return false;

        if (!TryParsePosition(originString, out Vector3? origin))
            return false;

        return Vector3.DistanceSquared(origin.Value, position) <= Math.Pow(radius, 2);
    }

    public object Clone()
    {
        return new Entity { Properties = Properties.Select(x => (EntityProperty)x.Clone()).ToList() };
    }

    public abstract class EntityProperty(string key) : ICloneable
    {
        public string Key { get; set; } = key;

        public abstract string ValueString { get; }

        public override string ToString()
        {
            return $"\"{Key}\" \"{ValueString}\"";
        }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static EntityProperty? Create(KeyValuePair<string, string> kv, int elVersion = 0)
        {
            return Create(kv.Key, kv.Value, elVersion);
        }

        public static EntityProperty? Create(string key, string value, int elVersion = 0)
        {
            if (!EntityIo.TryParse(value, out EntityIo? entityIo, elVersion))
                return new EntityProperty<string>(key, value);

            try
            {
                return new EntityProperty<EntityIo>(key, entityIo!);
            }
            catch (Exception e) when (e is IndexOutOfRangeException or FormatException)
            {
                Logger.Error($"Failed to parse entity IO value '{key}' '{value}'!");
                return null;
            }
        }

        public abstract object Clone();
    }

    /// <summary>
    /// Either a <![CDATA[ EntityProperty<string> or EntityPropery<EntityIo> ]]>
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public class EntityProperty<T>(string key, T value) : EntityProperty(key)
        where T : ICloneable
    {
        public T Value { get; set; } = value;

        public override string ValueString => Value.ToString() ?? "";

        public override object Clone()
        {
            return new EntityProperty<T>(Key, (T)Value.Clone());
        }
    }
}
