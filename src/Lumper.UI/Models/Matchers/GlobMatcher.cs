using System.Threading.Tasks;
using GlobExpressions;

namespace Lumper.UI.Models.Matchers;

/// <summary>
///     Pattern matcher that uses GLOB pattern matching
///     For more information check:
///     <see cref="!:https://en.wikipedia.org/wiki/Glob_(programming)" />
/// </summary>
public class GlobMatcher : Matcher
{
    private readonly Glob _matcher;

    public GlobMatcher(string pattern, bool ignoreCase = false)
        : base(pattern)
    {
        _matcher = new Glob(pattern,
            GlobOptions.Compiled | (ignoreCase
                ? GlobOptions.CaseInsensitive
                : GlobOptions.None));
    }

    public override ValueTask<bool> Match(string value)
    {
        return ValueTask.FromResult(_matcher.IsMatch(value.Trim()));
    }
}
