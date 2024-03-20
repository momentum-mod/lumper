namespace Lumper.UI.Models.Matchers;
using System.Threading.Tasks;
using GlobExpressions;

/// <summary>
///     Pattern matcher that uses GLOB pattern matching
///     For more information check:
///     <see cref="!:https://en.wikipedia.org/wiki/Glob_(programming)" />
/// </summary>
public class GlobMatcher(string pattern, bool isEmpty, bool ignoreCase = false) : Matcher(pattern, isEmpty)
{
    private readonly Glob _matcher = new(pattern,
        GlobOptions.Compiled | (ignoreCase
            ? GlobOptions.CaseInsensitive
            : GlobOptions.None));

    public override ValueTask<bool> Match(string value) => ValueTask.FromResult(_matcher.IsMatch(value.Trim()));
}
