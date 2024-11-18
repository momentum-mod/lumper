namespace Lumper.Lib.Bsp.Lumps;

public abstract class ManagedLump<T>(BspFile parent) : Lump<T>(parent)
    where T : System.Enum;
