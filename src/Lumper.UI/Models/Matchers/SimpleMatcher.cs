namespace Lumper.UI.Models.Matchers;
using System.Threading.Tasks;

/// <summary>
///     Pattern matcher that checks if pattern is contained inside matching string
/// </summary>
public class SimpleMatcher(string pattern, bool isEmpty) : Matcher(pattern, isEmpty)
{
    public override ValueTask<bool> Match(string value) => ValueTask.FromResult(value.Trim().Contains(Pattern));
}
