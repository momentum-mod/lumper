using System;
using System.Collections.Generic;
using System.Linq;

namespace MomBspTools.Lib.BSP.Struct
{
    public class Entity
    {
        public string ClassName
        {
            get => Properties.FirstOrDefault(property => property.Key == "classname").Value;
            set => Properties["classname"] = value;
        }
        
        public SortedDictionary<string, string> Properties { get; set; } = new();
        public List<KeyValuePair<string, EntityIO>> IOProperties { get; set; } = new();

        public Entity(string className)
        {
            _ = className ?? throw new ArgumentNullException(nameof(className));

            ClassName = className.Length == 0 ? className : throw new ArgumentException(className);
        }

        public Entity(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            foreach (var kv in keyValues)
            {
                var key = kv.Key;
                var value = kv.Value;

                if (key == "classname")
                    if (ClassName is not null)
                        Console.WriteLine("Found duplicate classname key, ignoring {0}", kv);
                

                if (EntityIO.IsIO(value))
                {
                    var props = value.Split(',');

                    try
                    {
                        var delay = float.Parse(props[3]);
                        var timestofire = int.Parse(props[4]);

                        IOProperties.Add(new KeyValuePair<string, EntityIO>(key,
                            new EntityIO
                            {
                                TargetEntityName = props[0], Input = props[1], Parameter = props[2], Delay = delay, TimesToFire = timestofire
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
                    Properties.Add(key, value);
                }
            }

            if (ClassName is null) Console.WriteLine("Warning: Found entity with missing classname! Fuck you!");
        }
    }
}