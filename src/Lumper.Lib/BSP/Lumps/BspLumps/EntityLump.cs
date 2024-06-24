namespace Lumper.Lib.BSP.Lumps.BspLumps;

using System.Collections.Generic;
using System.IO;
using System.Text;
using Enum;
using Lumps;
using NLog;
using Struct;

public class EntityLump(BspFile parent) : ManagedLump<BspLumpType>(parent)
{
    public HashSet<Entity> Data { get; set; } = [];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void Read(BinaryReader reader, long length)
        => Read(reader, length, false);

    /// <summary>
    /// Parse an entity lump using a BinaryReader on a stream.
    ///
    /// If `strict` is true, the reader will throw if it encounters an error.
    /// Otherwise, it simply logs the error and tries to continue.
    /// </summary>
    public void Read(BinaryReader reader, long length, bool strict)
    {
        while (ReadEntity(reader, reader.BaseStream.Position + length, strict))
        {
        }
    }

    private bool ReadEntity(BinaryReader reader, long endPos, bool strict)
    {
        var stringBuilder = new StringBuilder(512);
        var keyValues = new List<KeyValuePair<string, string>>();

        try
        {
            string? key = null;
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
                            throw new InvalidDataException("Closed unopened section");
                        if (!inString)
                        {
                            Data.Add(new Entity(keyValues) { EntityLumpVersion = Version });
                            return true;
                        }

                        break;

                    case '{':
                        if (inSection && !inString)
                            throw new InvalidDataException("Opened unclosed section");

                        if (!inString)
                            inSection = true;

                        break;

                    case '"':
                    {
                        if (!inSection)
                            throw new InvalidDataException("String in unopened section");

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
                                    Logger.Warn(
                                        $"Entity parser skipped value \"{value}\" for empty key, how u do dis?");
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
                            stringBuilder.Append(x);

                        break;
                }
            }
        }
        catch (InvalidDataException ex)
        {
            if (strict)
                throw;

            Logger.Error(ex,
                $"Failed to parse entity. Entity was {Data.Count} in list. Saving this BSP could cause data loss!");

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
                Logger.Error("End of entity not found! You have a seriously corrupted entity lump! Gosh!!");
        }

        return false;
    }

    public override void Write(Stream stream)
    {
        foreach (Entity ent in Data)
        {
            stream.Write("{\n"u8);

            foreach (Entity.EntityProperty prop in ent.Properties)
            {
                stream.Write(Encoding.ASCII.GetBytes(prop.ToString() + '\n'));
            }

            stream.Write("}\n"u8);
        }

        stream.Write("\0"u8);
    }

    public override bool Empty => Data.Count == 0;
}
