namespace Lumper.Lib.BSP.Lumps.BspLumps;

using System.Collections.Generic;
using System.IO;
using System.Text;
using Bsp.Enum;
using Enum;
using IO;
using Lumps;
using NLog;
using Struct;

public class EntityLump(BspFile parent) : ManagedLump<BspLumpType>(parent)
{
    public HashSet<Entity> Data { get; set; } = [];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
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
            while (reader.BaseStream.Position < reader.BaseStream.Length && (x = (char)reader.ReadByte()) != '\0')
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
                        else
                        {
                            stringBuilder.Append(x);
                        }

                        break;

                    case '{':
                        if (inSection && !inString)
                            throw new InvalidDataException("Opened unclosed section");

                        if (!inString)
                            inSection = true;
                        else
                            stringBuilder.Append(x);

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

                                if (key.Length == 0 && !EntityIo.TryParse(value, out _))
                                {
                                    Logger.Warn($"Parsed entity with value \"{value}\" with an empty key!");
                                }

                                keyValues.Add(new KeyValuePair<string, string>(key, value));

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

    public override void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null)
    {
        foreach (Entity ent in Data)
        {
            // 28591 is extended ASCII - https://en.wikipedia.org/wiki/Extended_ASCII
            // Encoding.ASCII is *7-bit* whereas 28591 is 8 bit - important as 8-bit ASCII is what the format uses
            stream.Write(Encoding.GetEncoding(28591).GetBytes("{\n"));
            foreach (Entity.EntityProperty prop in ent.Properties)
            {
                stream.Write(Encoding.GetEncoding(28591).GetBytes($"{prop}\n"));
            }

            stream.Write(Encoding.GetEncoding(28591).GetBytes("}\n"));
        }

        stream.Write(Encoding.GetEncoding(28591).GetBytes("\0"));
    }

    public override bool Empty => Data.Count == 0;
}
