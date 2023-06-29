using System.Threading.Tasks;

namespace Lumper.UI.Models.Matchers;

/// <summary>
///     Pattern matcher that checks if pattern is contained inside matching string
/// </summary>
public class SimpleMatcher : Matcher
{
    public SimpleMatcher(string pattern, bool isEmpty)
        : base(pattern, isEmpty)
    {
    }

    public override ValueTask<bool> Match(string value)
    {
        return ValueTask.FromResult(value.Trim().Contains(Pattern));
    }
}
