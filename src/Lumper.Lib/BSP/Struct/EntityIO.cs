namespace Lumper.Lib.Bsp.Struct;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public partial class EntityIo : ICloneable
{
    public string? TargetEntityName { get; set; }
    public string? Input { get; set; }
    public string? Parameter { get; set; }
    public float? Delay { get; set; }
    public int? TimesToFire { get; set; }

    private readonly char _separator;

    public EntityIo() { }

    public EntityIo(string value, char separator)
    {
        _separator = separator;
        string[] props = value.Split(separator);

        TargetEntityName = props[0];
        Input = props[1];
        Parameter = props[2];
        Delay = float.Parse(props[3]);
        if (int.TryParse(props[4], out int timesToFire))
        {
            TimesToFire = timesToFire;
        }
        else if (int.TryParse(props[4].Split(',')[0], out int timesToFire2))
        {
            // Edge case, occasionally tools that don't recognise ESC separators will put default comma-separated
            // values on the end of the ent IO string.
            // Source will still load be able to load the entity, since it parses using `atoi` which will ignore
            // everything after the first integer.
            TimesToFire = timesToFire2;
        }
        else
        {
            throw new InvalidDataException($"Failed to parse entity IO for string {value}");
        }
    }

    // Since VScript, \u001b (ESC) has been used as a separator.
    // It'd be faster to determine this once and pass that information around,
    // but would have to pass EntityLump into here or some other kind of spaghetti.
    // Note that on Strata we apparently set the entity lump version to 1, and always
    // use ESC, but again, don't want to pass more stuff around.
    // Discussed in #strata-tools on 11 June 2024
    public static bool TryParse(string value, out EntityIo? parsed, int elVersion = 0)
    {
        // ESC chars are deliberately hard to type so if we see 4, we're confident it's an ent IO, and it's def
        // not an ESC char-based separator.
        if (value.Count(s => s == '\u001b') == 4)
        {
            parsed = new EntityIo(value, '\u001b');
            return true;
        }

        // If a new strata BSP the entity lump version is 1 and separator char is *always* an ESC
        if (elVersion == 1)
        {
            parsed = null;
            return false;
        }

        // Fast comparison, since most stuff isn't entio
        if (value.Count(s => s == ',') != 4)
        {
            parsed = null;
            return false;
        }

        // Finally do slow check. This is annoying to do but otherwise strings like
        // a,b,c,d, can pass - bhop_flything had this issue.
        if (EntityIoRegex().IsMatch(value))
        {
            parsed = new EntityIo(value, ',');
            return true;
        }
        else
        {
            parsed = null;
            return false;
        }
    }

    public override string ToString() =>
        TargetEntityName + _separator + Input + _separator + Parameter + _separator + Delay + _separator + TimesToFire;

    public object Clone() => MemberwiseClone();

    // Match string,string,string,number,number where numbers can be decimals, negative, -.xxx
    [GeneratedRegex(@"^[^,]*," + @"[^,]*," + @"[^,]*" + @"(,(-?\d*\.?\d+)?){2}$")]
    private static partial Regex EntityIoRegex();
}
