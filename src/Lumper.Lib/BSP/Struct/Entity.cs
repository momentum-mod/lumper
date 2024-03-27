namespace Lumper.Lib.BSP.Struct;
using System;
using System.Collections.Generic;
using NLog;

public class Entity
{
    public abstract class EntityProperty(string key)
    {
        public string Key { get; set; } = key;
        public abstract string ValueString { get; set; }
        public override string ToString() => $"\"{Key}\" \"{ValueString}\"";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static EntityProperty? CreateProperty(KeyValuePair<string, string> kv) => CreateProperty(kv.Key, kv.Value);
        public static EntityProperty? CreateProperty(string key, string value)
        {
            if (!EntityIO.IsIO(value))
                return new EntityProperty<string>(key, value);

            var props = value.Split(',');
            try
            {
                var delay = float.Parse(props[3]);
                var timesToFire = int.Parse(props[4]);

                return new EntityProperty<EntityIO>(key,
                    new EntityIO
                    {
                        TargetEntityName = props[0],
                        Input = props[1],
                        Parameter = props[2],
                        Delay = delay,
                        TimesToFire = timesToFire
                    }
                    );
            }
            catch (Exception e) when (e is IndexOutOfRangeException or FormatException)
            {
                Logger.Error($"Failed to parse entity IO value '{key}' '{value}'!");
                return null;
            }
        }
    }

    public class EntityProperty<T>(string key, T value) : EntityProperty(key) where T : notnull
    {
        public T Value { get; set; } = value;
        public override string ValueString
        {
            get => Value.ToString()!;
            set => Value = (T)Convert.ChangeType(value, typeof(T));
        }
    }

    private readonly EntityProperty<string>? _className;
    public string ClassName
    {
        get => _className!.Value;
        set => _className!.Value = value;
    }

    public List<EntityProperty> Properties { get; set; } = [];

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Entity(IEnumerable<KeyValuePair<string, string>> keyValues)
    {
        foreach (KeyValuePair<string, string> kv in keyValues)
        {
            if (kv.Key == "classname")
            {
                if (_className is null)
                {
                    _className = new EntityProperty<string>(kv.Key, kv.Value);
                    Properties.Add(_className);
                    continue;
                }

                _logger.Warn($"Found duplicate classname key, ignoring. {kv}");
            }

            var prop = EntityProperty.CreateProperty(kv);
            if (prop is not null)
                Properties.Add(prop);
        }

        if (_className is null)
        {
            // After this line _className and ClassName are safe to mark ClassName as non-null
            ClassName = "<missing classname>";
            _logger.Warn("Found entity with missing classname!");
        }
    }
}
