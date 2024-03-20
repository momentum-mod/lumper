namespace Lumper.Lib.BSP.Lumps.BspLumps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lumper.Lib.BSP.Struct;

public class EntityLump(BspFile parent) : ManagedLump<BspLumpType>(parent)
{
    public List<Entity> Data { get; set; } = [];

    public override void Read(BinaryReader reader, long length)
    {
        while (ReadEntity(reader, reader.BaseStream.Position + length))
        { }
    }

    private bool ReadEntity(BinaryReader reader, long endPos)
    {
        var stringBuilder = new StringBuilder(512);
        var keyValues = new List<KeyValuePair<string, string>>();

        try
        {
            string key = null;
            var inSection = false;
            var inString = false;
            char x;
            while (reader.BaseStream.Position < reader.BaseStream.Length
                   && (x = reader.ReadChar()) != '\0')
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

            // Read to end of entity
            var foundEnd = false;
            while (reader.BaseStream.Position < endPos)
            {
                var c = reader.ReadChar();
                if (c is '\0' or '}')
                {
                    foundEnd = true;
                    break;
                }
            }
            if (!foundEnd)
                Console.WriteLine("WARNING: End of entity not found!");
        }

        return false;
    }

    public override void Write(Stream stream)
    {
        foreach (Entity ent in Data)
        {
            stream.Write(Encoding.ASCII.GetBytes("{\n"));
            foreach (Entity.Property prop in ent.Properties)
            {
                stream.Write(Encoding.ASCII.GetBytes($"{prop}\n"));
            }
            stream.Write(Encoding.ASCII.GetBytes("}\n"));
        }
        stream.Write(Encoding.ASCII.GetBytes("\0"));
    }
    public override bool Empty() => Data.Count == 0;
}
