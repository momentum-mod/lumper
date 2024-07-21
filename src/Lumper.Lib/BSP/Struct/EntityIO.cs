namespace Lumper.Lib.BSP.Struct;

using System;
using System.Linq;
using System.Text.RegularExpressions;

public partial class EntityIo : ICloneable
{
    public string? TargetEntityName { get; set; }
    public string? Input { get; set; }
    public string? Parameter { get; set; }
    public float? Delay { get; set; }
    public int? TimesToFire { get; set; }

    public EntityIo() { }

    /// <exception cref="IndexOutOfRangeException"/>
    /// <exception cref="FormatException"/>
    public EntityIo(string value, int elVersion = 0)
    {
        var separator = elVersion == 1 || value.Contains('\u001b') ? '\u001b' : ',';
        var props = value.Split(separator);

        TargetEntityName = props[0];
        Input = props[1];
        Parameter = props[2];
        Delay = float.Parse(props[3]);
        TimesToFire = int.Parse(props[4]);
    }

    // Since VScript, \u001b (ESC) has been used as a separator.
    // It'd be faster to determine this once and pass that information around,
    // but would have to pass EntityLump into here or some other kind of spaghetti.
    // Note that on Strata we apparently set the entity lump version to 1, and always
    // use ESC, but again, don't want to pass more stuff around.
    // Discussed in #strata-tools on 11 June 2024
    public static bool IsIo(string value, int elVersion = 0)
    {
        // If a new strata BSP the entity lump version is 1 and separator char is always \u001b (ESC)
        if (elVersion == 1)
            return value.Count(s => s == '\u001b') == 4;

        // ESC chars are deliberately hard to type so if we see 4, we're confident it's an ent IO, and it's def
        // not an ESC char-based separator.
        if (value.Count(s => s == '\u001b') == 4)
            return true;

        // Fast comparison, since most stuff isn't entio
        if (value.Count(s => s == ',') != 4)
            return false;

        // Finally do slow check. This is annoying to do but otherwise strings like
        // a,b,c,d, can pass - bhop_flything had this issue.
        return EntityIoRegex().IsMatch(value);
    }

    public override string ToString() => $"{TargetEntityName},{Input},{Parameter},{Delay},{TimesToFire}";

    public object Clone() => new EntityIo {
        TargetEntityName = TargetEntityName,
        Input = Input,
        Parameter = Parameter,
        Delay = Delay,
        TimesToFire = TimesToFire
    };

    // Match string,string,string,number,number where numbers can be decimals, negative, -.xxx
    [GeneratedRegex(@"^.*,.*,.*,(-?\d*\.?\d+)?,(-?\d*\.?\d+)?$")]
    private static partial Regex EntityIoRegex();
}
