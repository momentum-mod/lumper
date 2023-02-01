namespace Lumper.Lib.BSP.Lumps
{
    public abstract class ManagedLump<T> : Lump<T>
                                 where T : System.Enum
    {
        public ManagedLump(BspFile parent) : base(parent)
        {
        }
    }
}