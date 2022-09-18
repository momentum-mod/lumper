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
        }
        public class Property<T> : Property
        {
            public Property(string Key, T Value)
            {
                this.Key = Key;
                this.Value = Value;
            }
            public T Value { get; set; }
            public override string ToString() => $"\"{Key}\" \"{Value}\"";
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
                var key = kv.Key;
                var value = kv.Value;

                if (key == "classname")
                {
                    if (_ClassName is null)
                    {
                        _ClassName = new Property<string>(key, value);
                        Properties.Add(_ClassName);
                        continue;
                    }
                    else
                        Console.WriteLine("Found duplicate classname key, ignoring {0}", kv);
                }


                if (EntityIO.IsIO(value))
                {
                    var props = value.Split(',');

                    try
                    {
                        var delay = float.Parse(props[3]);
                        var timestofire = int.Parse(props[4]);

                        Properties.Add(new Property<EntityIO>(key,
                            new EntityIO
                            {
                                TargetEntityName = props[0],
                                Input = props[1],
                                Parameter = props[2],
                                Delay = delay,
                                TimesToFire = timestofire
                            }
                        ));
                    }
                    catch (Exception e) when (e is IndexOutOfRangeException || e is FormatException)
                    {
                        Console.WriteLine("Failed to pass entity IO value!");
                    }
                }
                else
                {
                    Properties.Add(new Property<string>(key, value));
                }
            }

            if (ClassName is null)
                Console.WriteLine("Warning: Found entity with missing classname!");
        }
    }
}