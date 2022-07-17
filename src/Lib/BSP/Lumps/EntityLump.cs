using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MomBspTools.Lib.BSP.Struct;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class EntityLump : ManagedLump
    {
        // this could also be set using generics but this class hierarchy is stupid enough already
        public List<Entity> Data { get; set; } = new();

        public override void Read(BinaryReader reader)
        {
            while (ReadEntity(reader)) { }
        }

        private bool ReadEntity(BinaryReader reader)
        {
            var stringBuilder = new StringBuilder(512);
            var keyValues = new List<KeyValuePair<string, string>>();

            try
            {
                string key = null;
                var inSection = false;
                var inString = false;
                char x;
                while ((x = reader.ReadChar()) != '\0')
                {
                    switch (x)
                    {
                        case '}':
                            if (!inSection && !inString)
                            {
                                throw new InvalidDataException("Closed unopened section");
                            }

                            if (!inString)
                            {
                                Data.Add(new Entity(keyValues));
                                return true;
                            }

                            break;

                        case '{':
                            if (inSection && !inString)
                            {
                                throw new InvalidDataException("Opened unclosed section");
                            }

                            if (!inString)
                            {
                                inSection = true;
                            }

                            break;

                        case '"':
                            {
                                if (!inSection)
                                {
                                    throw new InvalidDataException("String in unopened section");
                                }

                                if (inString)
                                {
                                    if (key == null)
                                    {
                                        key = stringBuilder.ToString();
                                    }
                                    else
                                    {
                                        var value = stringBuilder.ToString();

                                        if (key.Length == 0)
                                        {
                                            Console.WriteLine(
                                                "Entity parser skipped value \"{0}\" for empty key, how u do dis?", value);
                                        }

                                        else
                                        {
                                            keyValues.Add(new KeyValuePair<string, string>(key, value));
                                        }

                                        key = null;
                                    }

                                    stringBuilder.Remove(0, stringBuilder.Length);
                                }

                                inString = !inString;
                                continue;
                            }

                        default:
                            if (inSection && inString)
                            {
                                stringBuilder.Append(x);
                            }

                            break;
                    }
                }
            }
            catch (InvalidDataException e)
            {
                Console.WriteLine(
                    "WARNING: Failed to parse entity: {0}, {1} in list.\n Saving this BSP could cause data loss!",
                    e.Message, Data.Count);

                // Read to end of entity (barf)
                for (var c = ' '; c != '\0' && c != '}'; c = reader.ReadChar())
                {
                }
            }

            return false;
        }

        public override void Write(BinaryWriter writer)
        {
            foreach (var ent in Data)
            {
                writer.Write("{");
                foreach (var (key, value) in ent.Properties)
                    writer.Write($"\"{key}\" \"{value}\"");
                foreach (var (key, value) in ent.IOProperties)
                    writer.Write($"\"{key}\" \"{value.TargetEntityName},{value.Input},{value.Parameter},{value.Delay},{value.TimesToFire}\"");
                writer.Write("}\0");
            }
        }

        public EntityLump(BspFile parent) : base(parent)
        {
        }
    }
}