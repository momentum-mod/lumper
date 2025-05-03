namespace Lumper.Test;

using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.Lumps.BspLumps;

public static class TestUtils
{
    /// <summary>
    /// Create a mock BspFile for testing purposes.
    /// Feel free to add more lumps to the mock as needed.
    /// </summary>
    public static BspFile CreateMockBspFile()
    {
        // Ctor is private
        var bspFile = (BspFile)Activator.CreateInstance(typeof(BspFile), true)!;
        var entityLump = new EntityLump(bspFile);
        var pakfileLump = new PakfileLump(bspFile);

        bspFile.Lumps[BspLumpType.Entities] = entityLump;
        bspFile.Lumps[BspLumpType.Pakfile] = pakfileLump;

        return bspFile;
    }
}
