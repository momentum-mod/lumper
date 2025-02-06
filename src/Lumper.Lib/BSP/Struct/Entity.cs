namespace Lumper.Lib.Bsp.Struct;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;

public partial class Entity : ICloneable
{
    public Entity()
        : this(null) { }

    public int EntityLumpVersion { get; init; }

    public List<EntityProperty> Properties { get; set; }

    public Entity(IEnumerable<KeyValuePair<string, string>>? kv) =>
        Properties = kv is not null
            ? kv.Select(x => EntityProperty.Create(x, EntityLumpVersion)).OfType<EntityProperty>().ToList()
            : [];

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


    [GeneratedRegex(@"^\*\d+$")]
    private static partial Regex BrushEntityRegex();

    public bool IsBrushEntity =>
        Properties.FirstOrDefault(x => x.Key == "model") is EntityProperty<string> { Value: string modelString }
        && BrushEntityRegex().IsMatch(modelString);
    public object Clone() => new Entity { Properties = Properties.Select(x => (EntityProperty)x.Clone()).ToList() };

    public abstract class EntityProperty(string key) : ICloneable
    {
        public string Key { get; set; } = key;

        public abstract string? ValueString { get; }

        public override string ToString() => $"\"{Key}\" \"{ValueString}\"";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static EntityProperty? Create(KeyValuePair<string, string> kv, int elVersion = 0) =>
            Create(kv.Key, kv.Value, elVersion);

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
    public class EntityProperty<T>(string key, T? value) : EntityProperty(key)
        where T : ICloneable
    {
        public T? Value { get; set; } = value;

        public override string? ValueString => Value?.ToString();

        public override object Clone() => new EntityProperty<T>(Key, (T?)Value?.Clone());
    }
}
