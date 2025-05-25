namespace Lumper.Test;

using System.Reflection;
using Lib.AssetManifest;
using Lib.Bsp.Lumps.BspLumps;
using Lib.Bsp.Struct;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Enum;

/// <summary>
/// Collection of utility methods for unit/integration testing purposes.
///
/// We're using actual instances here rather than mocks, patching the instances using reflection
/// when necessary. Mocking everything using Moq causes endless issues with non-virtual
/// methods/properties, private constructors etc. and I'd rather use reflection than modify
/// accessiblity just for the sake of unit testing.
/// </summary>
public static class TestUtils
{
    /// <summary>
    /// Create a mock BspFile for testing purposes.
    /// </summary>
    public static BspFile CreateMockBspFile()
    {
        // Using an actual stream rather than hijacking private ctor with Activator.CreateInstance.
        // TexData lumps are complex to set up, easier to let library do it.
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, BspFile.Encoding, true);

        writer.Write("VBSP"u8.ToArray());

        // Version
        writer.Write(20);

        const int gameLumpHeaderSize = 17; // 1 + single entry for Sprp (16)

        // Main header
        for (int i = 0; i < BspFile.HeaderLumps; i++)
        {
            if (i == (int)BspLumpType.GameLump)
            {
                writer.Write(BspFile.HeaderSize); // Offset
                writer.Write(gameLumpHeaderSize); // Length
                writer.Write(0); // Version
                writer.Write(0); // FourCC
            }
            else
            {
                writer.Write(0); // Offset
                writer.Write(0); // Length
                writer.Write(0); // Version
                writer.Write(0); // FourCC
            }
        }

        // Revision
        writer.Write(1);

        // Put game lump right after main header
        stream.Position = BspFile.HeaderSize;
        writer.Write(1); // Number of game lumps

        // Write a proper Sprp lump header
        writer.Write((int)GameLumpType.sprp); // Game lump ID (Sprp)
        writer.Write((ushort)0); // Flags
        writer.Write((ushort)7); // Version
        writer.Write(BspFile.HeaderSize + gameLumpHeaderSize); // Offset
        writer.Write(3 * 4); // Length, 4 bytes each for dict, leaf, and static prop entries

        // Now we're in Sprp data
        writer.Write(0); // Dict entries
        writer.Write(0); // Leaf entries
        writer.Write(0); // Static prop entries

        return BspFile.FromStream(stream, null)!;
    }

    /// <summary>
    /// Create an instance of the underlying AssetManifest.Manifest dictionary,
    /// patch it into the static asset manifest, and return it.
    /// </summary>
    public static Dictionary<string, List<AssetManifest.Asset>> GetMutableAssetManifest()
    {
        // Create test dictionary
        var testManifest = new Dictionary<string, List<AssetManifest.Asset>>();

        // Get the LazyManifest field
        FieldInfo? lazyManifestField =
            typeof(AssetManifest).GetField("LazyManifest", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find LazyManifest field");

        // Get the Lazy<> instance
        object? lazyInstance = lazyManifestField.GetValue(null);

        // Patch the lazy value
        PatchLazyValue(lazyInstance!, testManifest);

        return testManifest;
    }

    public static PakfileEntry AddPakfileEntry(PakfileLump pakfileLump, string path, string data = "")
    {
        // Create a new PakfileEntry with the given path and data
        var entry = new PakfileEntry(pakfileLump, path, new MemoryStream(BspFile.Encoding.GetBytes(data)));

        // Add the entry to the PakfileLump's entries
        pakfileLump.Entries.Add(entry);

        return entry;
    }

    /// <summary>
    /// Add a PakfileEntry to the given PakfileLump and patch its hash.
    /// </summary>
    public static PakfileEntry AddPakfileEntryWithHash(
        PakfileLump pakfileLump,
        string hash,
        string path,
        string data = ""
    )
    {
        var entry = new PakfileEntry(pakfileLump, path, new MemoryStream(BspFile.Encoding.GetBytes(data)));
        pakfileLump.Entries.Add(entry);

        typeof(PakfileEntry).GetField("_hash", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(entry, hash);

        return entry;
    }

    /// <summary>
    /// Patches the underlying value of a Lazy of T instance.
    /// </summary>
    /// <typeparam name="T">The type contained in the Lazy instance</typeparam>
    /// <param name="lazyInstance">The Lazy instance to patch</param>
    /// <param name="newValue">The new value to inject</param>
    /// <returns>The new value that was set</returns>
    public static T PatchLazyValue<T>(object lazyInstance, T newValue)
    {
        // Access the _value field
        FieldInfo? valueField = lazyInstance
            .GetType()
            .GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);

        // Access the _isValueCreated field (or _state field depending on implementation)
        FieldInfo? isValueCreatedField = lazyInstance
            .GetType()
            .GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);

        if (valueField == null)
            throw new InvalidOperationException("Could not find _value field in Lazy<T>");

        // Set the value
        valueField.SetValue(lazyInstance, newValue);

        // Mark as initialized by setting _state to null
        isValueCreatedField?.SetValue(lazyInstance, null);

        return newValue;
    }
}
