using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumper.Lib.BSP.Struct
{
    public class Entity
    {
        public abstract class Property
        {
            public string Key { get; protected set; }
            public abstract string ValueString { get; set; }
            public override string ToString() => $"\"{Key}\" \"{ValueString}\"";

            public static Property CreateProperty(KeyValuePair<string, string> kv)
            {
                return CreateProperty(kv.Key, kv.Value);
            }
            public static Property CreateProperty(string key, string value)
            {
                if (EntityIO.IsIO(value))
                {
                    var props = value.Split(',');

                    try
                    {
                        var delay = float.Parse(props[3]);
                        var timestofire = int.Parse(props[4]);

                        return new Property<EntityIO>(key,
                            new EntityIO
                            {
                                TargetEntityName = props[0],
                                Input = props[1],
                                Parameter = props[2],
                                Delay = delay,
                                TimesToFire = timestofire
                            }
                        );
                    }
                    catch (Exception e) when (e is IndexOutOfRangeException || e is FormatException)
                    {
                        Console.WriteLine($"Failed to pass entity IO value '{key}' '{value}'!");
                    }
                }
                return new Property<string>(key, value);
            }
        }
        public class Property<T> : Property
        {
            public Property(string Key, T Value)
            {
                this.Key = Key;
                this.Value = Value;
            }
            public T Value { get; set; }
            public override string ValueString
            {
                get { return Value.ToString(); }
                set { Value = (T)Convert.ChangeType(value, typeof(T)); }
            }
        }
        private readonly Property<string> _ClassName;
        public string ClassName
        {
            get { return _ClassName.Value; }
            set { _ClassName.Value = value; }
        }
        public List<Property> Properties { get; set; } = new();

        public Entity(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            foreach (var kv in keyValues)
            {
                if (kv.Key == "classname")
                {
                    if (_ClassName is null)
                    {
                        _ClassName = new Property<string>(kv.Key, kv.Value);
                        Properties.Add(_ClassName);
                        continue;
                    }
                    else
                        Console.WriteLine("Found duplicate classname key, ignoring {0}", kv);
                }
                Properties.Add(Property.CreateProperty(kv));
            }

            if (ClassName is null)
                Console.WriteLine("Warning: Found entity with missing classname!");
        }
    }
}