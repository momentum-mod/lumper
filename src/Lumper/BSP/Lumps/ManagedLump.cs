namespace Lumper.Lib.BSP.Lumps;

public abstract class ManagedLump<T>(BspFile parent) : Lump<T>(parent)
                             where T : System.Enum
{
}
