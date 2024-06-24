namespace Lumper.UI.Models.Matchers;

/// <summary>
/// Pattern matcher that checks if pattern is contained inside matching string
/// </summary>
public class SimpleMatcher(string pattern, bool isEmpty) : Matcher(pattern, isEmpty)
{
    public override bool Match(string? value) => value is not null && value.Trim().Contains(Pattern);
}
