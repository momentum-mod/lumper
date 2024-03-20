namespace Lumper.UI.Models;
using System.Threading.Tasks;

/// <summary>
///     Base class for pattern matching
/// </summary>
public abstract class Matcher
{
    //todo meh
    //IsEmpty needs to be seperate because the pattern 
    //could have been changed by the matcher
    protected Matcher(string pattern, bool isEmpty)
    {
        Pattern = pattern;
        IsEmpty = isEmpty;
    }

    protected string Pattern
    {
        get;
    }

    public bool IsEmpty
    {
        get;
    }

    public abstract ValueTask<bool> Match(string value);
}
