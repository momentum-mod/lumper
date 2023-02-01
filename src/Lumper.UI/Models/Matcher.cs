using System.Threading.Tasks;

namespace Lumper.UI.Models;

/// <summary>
///     Base class for pattern matching
/// </summary>
public abstract class Matcher
{
    protected Matcher(string pattern)
    {
        Pattern = pattern;
    }

    protected string Pattern
    {
        get;
    }

    public abstract ValueTask<bool> Match(string value);
}
