namespace Lumper.UI;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// I keep wanting a class for random utilities and not doing it because I don't have
/// enough stuff to justify it existing. So fuck it, I'm just doing it!
/// </summary>
public static class Utils
{
    /// <summary>
    /// Expand flags/bitfields of T to IEnumerable&lt;T&gt;
    /// </summary>
    public static IEnumerable<T> ExpandBitfield<T>(T flags)
        where T : Enum
    {
        int flagInt = (int)Convert.ChangeType(flags, typeof(int), CultureInfo.InvariantCulture);
        return Enum.GetValues(typeof(T))
            .Cast<uint>()
            .Where(f => (f & flagInt) != 0)
            .Select(x => (T)Enum.ToObject(typeof(T), x));
    }
}
