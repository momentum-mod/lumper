namespace Lumper.Test;

using Lib.Bsp.Enum;
using Lumper.Lib.Bsp;

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
}
