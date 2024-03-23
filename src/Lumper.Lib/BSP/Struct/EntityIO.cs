// ReSharper disable InconsistentNaming
namespace Lumper.Lib.BSP.Struct;
using System.Linq;

public class EntityIO
{
    public string? TargetEntityName { get; set; }
    public string? Input { get; set; }
    public string? Parameter { get; set; }
    public float Delay { get; set; }
    public int TimesToFire { get; set; }

    public static bool IsIO(string value) => value.Count(s => s == ',') == 4;
    public override string ToString() => $"{TargetEntityName},{Input},{Parameter},{Delay},{TimesToFire}";
}
