namespace Lumper.UI.Models.Matchers;

using GlobExpressions;

/// <summary>
/// Pattern matcher that uses glob pattern matching
/// </summary>
public class GlobMatcher(string pattern, bool isEmpty, bool ignoreCase = false) : Matcher(pattern, isEmpty)
{
    private readonly Glob _matcher = new(pattern,
        GlobOptions.Compiled | (ignoreCase
            ? GlobOptions.CaseInsensitive
            : GlobOptions.None));

    public override bool Match(string? value) => value is not null && _matcher.IsMatch(value.Trim());
}
