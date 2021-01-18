using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace MomBspTools.Lib.VTF
{
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

        private readonly List<KeyValuePair<string, string>> _resourceTags = new List<KeyValuePair<string,string>>()
        {
            new ("\x01\0\0", "Thumbnail"),
            new ("\x30\0\0", "High Res Image"),
            new ("\x10\0\0", "Animated Particle Sheet"),
            new ("CRC", "CRC Data"),
            new ("LOD", "Level of Detail"),
            new ("TSO", "Extended Custom Flags"),
            new ("KVD", "Arbitrary KeyValues")
        };

        private readonly float[] _reflectivity = new float[3];
        public ReadOnlyCollection<float> Reflectivity => Array.AsReadOnly(_reflectivity);
        
        private readonly List<KeyValuePair<string, string>> _keyValuePairs = new();
        public IEnumerable<KeyValuePair<string, string>> KeyValuePairs => _keyValuePairs.AsEnumerable();
        
        private readonly List<string> _tags = new();
        public IEnumerable<string> Tags => _tags.AsEnumerable();
        
        private readonly List<string> _flags = new();
        public IEnumerable<string> Flags => _flags.AsEnumerable();

        public VtfFile(string path)
        {
            ParseHeader(path);
        }

        private void ParseHeader(string path)
        {
            using var vtfFile = new BinaryReader(File.Open(path,FileMode.Open));
            
            Console.WriteLine($"Opening: {path}");
            
            const string headerSignature = "VTF\0";
            var inputSignature = Encoding.Default.GetString(vtfFile.ReadBytes(4));
            
            if (inputSignature == headerSignature)
            {
                VersionMajor = vtfFile.ReadInt32();
                VersionMinor = vtfFile.ReadInt32();
                Console.WriteLine($"VTF Version {VersionMajor}.{VersionMinor}");
                
                _headerSize = vtfFile.ReadInt32();
                Console.WriteLine($"Header Size: {_headerSize} Bytes");
                
                LargestMipmapWidth = vtfFile.ReadInt16();
                LargestMipmapHeight = vtfFile.ReadInt16();
                Console.WriteLine($"Texture Dimensions: {LargestMipmapWidth} X {LargestMipmapHeight}");
                
                ParseFlags(vtfFile);
                
                AmountOfFrames = vtfFile.ReadInt16();
                Console.WriteLine($"Amount of Frames: {AmountOfFrames}");
                
                FirstFrame = vtfFile.ReadInt16();
                Console.WriteLine($"First Frame: {FirstFrame}");
                
                // Skip padding of 4 bytes.
                vtfFile.ReadBytes(4);
                
                ParseReflectivity(vtfFile);
                
                // Skip padding of 4 bytes.
                vtfFile.ReadBytes(4);
                
                BumpmapScale = vtfFile.ReadSingle();
                Console.WriteLine($"Bumpmap Scale: {BumpmapScale}");
                
                HighResolutionImageFormat = vtfFile.ReadInt32();
                Console.WriteLine($"Texture Format: {(ImageFormats)HighResolutionImageFormat}");
                
                AmountOfMipmaps = vtfFile.ReadByte();
                Console.WriteLine($"Amount of Mipmaps: {AmountOfMipmaps}");
                
                LowResolutionImageFormat = vtfFile.ReadUInt32();
                Console.WriteLine($"Thumbnail Format: {(ImageFormats)LowResolutionImageFormat}");
                
                LowResolutionImageWidth = vtfFile.ReadByte();
                LowResolutionImageHeight = vtfFile.ReadByte();
                Console.WriteLine($"Thumbnail Dimensions: {LowResolutionImageWidth} X {LowResolutionImageHeight}");
                
                ParseDepthAndResources(vtfFile);
            }
            else
            {
                throw new ArgumentException("File is not a valid VTF, file signature did not match", path);
            }
        }

        private void ParseFlags(BinaryReader vtfFile)
        {
            _rawFlags = vtfFile.ReadUInt32();
            if (_rawFlags != 0)
            {
                Console.WriteLine($"This VTF has the following flags: 0x{_rawFlags:x8}");
                foreach (uint currentFlag in Enum.GetValues(typeof(VtfFlags)))
                {
                    if ((currentFlag & _rawFlags) == 0) continue;
                    
                    Console.WriteLine($"- {(VtfFlags)currentFlag,-30} (0x{currentFlag:x8})");
                    
                    // Couldn't figure out how to do this in one line.
                    var castFlag = (VtfFlags)currentFlag;
                    _flags.Add(nameof(currentFlag));
                }
            }
            else
            {
                Console.WriteLine("No Flags found.");
            }
        }

        private void ParseReflectivity(BinaryReader vtfFile)
        {
            for (var i = 0; i < 3; i++)
            {
                _reflectivity[i] = vtfFile.ReadSingle();
            }
            
            Console.WriteLine($"Reflectivity: {_reflectivity[0]} {_reflectivity[1]} {_reflectivity[2]}");
        }

        private void ParseKeyValues(string keyValues)
        {
            // TODO: Can KVs even have quotation marks?
            string[] keyValueSplitChars = {"\n", "\t", "\r", "\"", " ", "{", "}"};
            var splitKeyValues = keyValues.Split(keyValueSplitChars, StringSplitOptions.RemoveEmptyEntries);
            
            // Remove the "Information" part from the array as it's not a KeyValue.
            splitKeyValues = splitKeyValues.Skip(1).ToArray();
            
            var parsingKey = true;
            var entryRepeat = 0;

            string entryKey = null;
            string entryValue = null;
            
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
                    _keyValuePairs.Add(new KeyValuePair<string, string>(entryKey, entryValue));
                    entryRepeat = 0;
                }
            }
        }

        private void ParseDepthAndResources(BinaryReader vtfFile)
        {
            if (VersionMinor < 2) return;
            
            TextureDepth = vtfFile.ReadInt16();
            Console.WriteLine($"Texture Depth: {TextureDepth}");
            
            if (VersionMinor < 3) return;
            
            // Skip 3 Bytes.
            vtfFile.ReadBytes(3);
            
            _numberOfResources = vtfFile.ReadInt32();
            Console.WriteLine($"Number of Resources: {_numberOfResources}");
            
            // Skip 8 Bytes.
            vtfFile.ReadBytes(8);
            
            for (var i = 0; i < _numberOfResources; i++)
            {
                var inputTag = Encoding.Default.GetString(vtfFile.ReadBytes(3));

                foreach (var (key, value) in _resourceTags)
                {
                    if (inputTag != key) continue;
                    
                    Console.WriteLine($"- {value}");
                    _tags.Add(value);
                }

                // Skip Resource flag, it is unused.
                vtfFile.ReadByte();

                // Is this the place to print this information?
                if (inputTag == "LOD")
                {
                    var lodU = vtfFile.ReadByte();
                    var lodV = vtfFile.ReadByte();
                    Console.WriteLine($"  - Clamp U: {lodU}\n  - Clamp V: {lodV}");
                    
                    // Skip remainder bytes as the LOD values are in 2 bytes, not 4.
                    vtfFile.ReadBytes(2);
                }
                else if (inputTag == "KVD")
                {
                    var resourceOffset = vtfFile.ReadInt32();
                    
                    // Move ahead in the file by the offset given minus the header size, and nudged forward by 4 bytes.
                    vtfFile.ReadBytes((resourceOffset - _headerSize) + 4);

                    string keyValues = null;
                    while (vtfFile.BaseStream.Position != vtfFile.BaseStream.Length)
                    {
                        // 64 Bytes is arbitrary number, better suggestion?
                        keyValues += Encoding.Default.GetString(vtfFile.ReadBytes(64));
                    }
                    
                    ParseKeyValues(keyValues);
                }
                else
                {
                    // Nothing to be used, skip the 4 bytes.
                    vtfFile.ReadBytes(4);
                }
            }
        }

        /// <summary>
        /// Returns if the VTF is a compressed (DXT) Format.
        /// </summary>
        public bool IsCompressedFormat()
        {
            // Entries 13 to 15 in the ImageFormat enum are DXT, the rest aren't compressed.
            return (HighResolutionImageFormat > 12 && HighResolutionImageFormat < 16);
        }
        
        public bool IsAnimated()
        {
            return AmountOfFrames > 1;
        }
        
        public bool HasMipmaps()
        {
            return AmountOfMipmaps != 0;
        }
        
        public bool HasThumbnail()
        {
            return (ImageFormats)LowResolutionImageFormat != ImageFormats.IMAGE_FORMAT_NONE;
        }
        
        public bool HasFlags()
        {
            return _rawFlags != 0;
        }
        
        public bool HasKeyValues()
        {
            return KeyValuePairs.Any();
        }
    }
}