using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MomBspTools.Lib.BSP.Struct;

namespace MomBspTools.Lib.BSP.Lumps
{
    public class EntityLump : ManagedLump
    {
        public List<Entity> Data { get; set; } = new();

        public override void Read(BinaryReader r)
        {
            while (ReadItem(r))
            {
            }
        }

        private bool ReadItem(BinaryReader r)
        {
            var stringBuilder = new StringBuilder(512);
            var keyValues = new List<KeyValuePair<string, string>>();
            
            try
            {
                string key = null;
                var inSection = false;
                var inString = false;
                char x;
                while ((x = r.ReadChar()) != '\0')
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
                for (var c = ' '; c != '\0' && c != '}'; c = r.ReadChar())
                {
                }
            }

            return false;
        }

        public override void Write(BinaryWriter r)
        {
            throw new NotImplementedException();
        }

        public EntityLump(BspFile parent) : base(parent)
        {
        }
    }
}