namespace Lumper.Lib.VTF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Enum;

public class VtfFile
{
    public int VersionMajor { get; set; }
    public int VersionMinor { get; set; }
    public short LargestMipmapWidth { get; set; }
    public short LargestMipmapHeight { get; set; }
    public short AmountOfFrames { get; set; }
    public short FirstFrame { get; set; }
    public float BumpmapScale { get; set; }
    public int HighResolutionImageFormat { get; set; }
    public int AmountOfMipmaps { get; set; }
    public uint LowResolutionImageFormat { get; set; } // This var is unsigned to allow comparison against 0xFFFFFFFF to check a thumbnail exists.
    public short LowResolutionImageWidth { get; set; }
    public short LowResolutionImageHeight { get; set; }
    public int TextureDepth { get; set; }

    private int _headerSize;
    private uint _rawFlags;
    private int _numberOfResources;

    private static readonly Dictionary<string, string> _resourceTags = new()
    {
        { "\x01\0\0", "Thumbnail" },
        { "\x30\0\0", "High Res Image" },
        { "\x10\0\0", "Animated Particle Sheet" },
        { "CRC",      "CRC Data" },
        { "LOD",      "Level of Detail" },
        { "TSO",      "Extended Custom Flags" },
        { "KVD",      "Arbitrary KeyValues" }
    };

    private readonly float[] _reflectivity = new float[3];

    public List<KeyValuePair<string, string>> KeyValuePairs { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<VtfFlags> Flags { get; set; } = [];

    public VtfFile(string path) => ParseHeader(path);

    private void ParseHeader(string path)
    {
        using var reader = new BinaryReader(File.Open(path, FileMode.Open));

        Console.WriteLine($"Opening: {path}");

        const string headerSignature = "VTF\0";
        var inputSignature = Encoding.Default.GetString(reader.ReadBytes(4));

        if (inputSignature != headerSignature)
            throw new ArgumentException("File is not a valid VTF, file signature did not match", path);

        VersionMajor = reader.ReadInt32();
        VersionMinor = reader.ReadInt32();
        Console.WriteLine($"VTF Version {VersionMajor}.{VersionMinor}");

        _headerSize = reader.ReadInt32();
        Console.WriteLine($"Header Size: {_headerSize} Bytes");

        LargestMipmapWidth = reader.ReadInt16();
        LargestMipmapHeight = reader.ReadInt16();
        Console.WriteLine($"Texture Dimensions: {LargestMipmapWidth} X {LargestMipmapHeight}");

        ParseFlags(reader);

        AmountOfFrames = reader.ReadInt16();
        Console.WriteLine($"Amount of Frames: {AmountOfFrames}");

        FirstFrame = reader.ReadInt16();
        Console.WriteLine($"First Frame: {FirstFrame}");

        // Skip padding of 4 bytes.
        reader.ReadBytes(4);

        ParseReflectivity(reader);

        // Skip padding of 4 bytes.
        reader.ReadBytes(4);

        BumpmapScale = reader.ReadSingle();
        Console.WriteLine($"Bumpmap Scale: {BumpmapScale}");

        HighResolutionImageFormat = reader.ReadInt32();
        Console.WriteLine($"Texture Format: {(ImageFormats)HighResolutionImageFormat}");

        AmountOfMipmaps = reader.ReadByte();
        Console.WriteLine($"Amount of Mipmaps: {AmountOfMipmaps}");

        LowResolutionImageFormat = reader.ReadUInt32();
        Console.WriteLine($"Thumbnail Format: {(ImageFormats)LowResolutionImageFormat}");

        LowResolutionImageWidth = reader.ReadByte();
        LowResolutionImageHeight = reader.ReadByte();
        Console.WriteLine($"Thumbnail Dimensions: {LowResolutionImageWidth} X {LowResolutionImageHeight}");

        ParseDepthAndResources(reader);
    }

    private void ParseFlags(BinaryReader reader)
    {
        _rawFlags = reader.ReadUInt32();
        if (_rawFlags == 0)
        {
            Console.WriteLine("No Flags found.");
            return;
        }

        Console.WriteLine($"VTF has the following flags: 0x{_rawFlags:x8}");
        foreach (uint currentFlag in System.Enum.GetValues(typeof(VtfFlags)))
        {
            if ((currentFlag & _rawFlags) == 0)
                continue;

            Console.WriteLine($"- {(VtfFlags)currentFlag,-30} (0x{currentFlag:x8})");

            Flags.Add((VtfFlags)currentFlag);
        }
    }

    private void ParseReflectivity(BinaryReader reader)
    {
        for (var i = 0; i < 3; i++)
            _reflectivity[i] = reader.ReadSingle();

        Console.WriteLine($"Reflectivity: {_reflectivity[0]} {_reflectivity[1]} {_reflectivity[2]}");
    }

    private void ParseKeyValues(string reader)
    {
        // TODO: Does this actually work? What format actually is this stuff?
        string[] keyValueSplitChars = ["\n", "\t", "\r", "\"", " ", "{", "}"];
        var splitKeyValues = reader.Split(keyValueSplitChars, StringSplitOptions.RemoveEmptyEntries);

        // Remove the "Information" part from the array as it's not a KeyValue.
        splitKeyValues = splitKeyValues.Skip(1).ToArray();

        var parsingKey = true;
        var entryRepeat = 0;

        string? entryKey = null;
        string? entryValue = null;

        foreach (var currentEntry in splitKeyValues)
        {
            if (parsingKey)
            {
                Console.Write($"  - {currentEntry}: ");
                entryKey = currentEntry;
                parsingKey = false;
                entryRepeat++;
            }
            else
            {
                Console.WriteLine($"{currentEntry}");
                entryValue = currentEntry;
                parsingKey = true;
                entryRepeat++;
            }

            if (entryRepeat == 2)
            {
                KeyValuePairs.Add(new KeyValuePair<string, string>(entryKey!, entryValue!));
                entryRepeat = 0;
            }
        }
    }

    private void ParseDepthAndResources(BinaryReader reader)
    {
        if (VersionMinor < 2)
            return;

        TextureDepth = reader.ReadInt16();
        Console.WriteLine($"Texture Depth: {TextureDepth}");

        if (VersionMinor < 3)
            return;

        // Skip 3 Bytes.
        reader.ReadBytes(3);

        _numberOfResources = reader.ReadInt32();
        Console.WriteLine($"Number of Resources: {_numberOfResources}");

        // Skip 8 Bytes.
        reader.ReadBytes(8);

        for (var i = 0; i < _numberOfResources; i++)
        {
            var inputTag = Encoding.ASCII.GetString(reader.ReadBytes(3));

            if (_resourceTags.TryGetValue(inputTag, out var tag))
            {
                Console.WriteLine($"- {tag}");
                Tags.Add(tag);
            }
            else
            {
                Console.WriteLine($"Invalid tag {tag}");
            }

            // Skip Resource flag, it is unused.
            reader.ReadByte();

            // Is this the place to print this information?
            switch (inputTag)
            {
                case "LOD":
                {
                    var lodU = reader.ReadByte();
                    var lodV = reader.ReadByte();
                    Console.WriteLine($"  - Clamp U: {lodU}\n  - Clamp V: {lodV}");

                    // Skip remainder bytes as the LOD values are in 2 bytes, not 4.
                    reader.ReadBytes(2);
                    break;
                }
                case "KVD":
                {
                    var resourceOffset = reader.ReadInt32();

                    // Move ahead in the file by the offset given minus the header size, and nudged forward by 4 bytes.
                    reader.ReadBytes(resourceOffset - _headerSize + 4);


                    // TODO: Cabbage's original code makes no sense to me, so not sure this is ever getting hit
                    // try in a debugger. This is the original:
                    // string? keyValues = null;
                    // while (reader.BaseStream.Position != reader.BaseStream.Length)
                    // {
                    //     // 64 Bytes is arbitrary number, better suggestion?
                    //     keyValues += Encoding.ASCII.GetString(reader.ReadBytes(64));
                    // }

                    var keyValues = Encoding.ASCII.GetString(reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position)));

                    ParseKeyValues(keyValues);
                    break;
                }
                default:
                    // Nothing to be used, skip the 4 bytes.
                    reader.ReadBytes(4);
                    break;
            }
        }
    }

    /// <summary>
    /// Returns if the VTF is a compressed (DXT) Format.
    /// </summary>
    public bool IsCompressedFormat() =>
        // Entries 13 to 15 in the ImageFormat enum are DXT, the rest aren't compressed.
        HighResolutionImageFormat is > 12 and < 16;

    public bool IsAnimated() => AmountOfFrames > 1;

    public bool HasMipmaps() => AmountOfMipmaps != 0;

    public bool HasThumbnail() => (ImageFormats)LowResolutionImageFormat != ImageFormats.IMAGEFORMATNONE;

    public bool HasFlags() => _rawFlags != 0;

    public bool HasKeyValues() => KeyValuePairs.Count > 0;
}
