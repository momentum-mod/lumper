namespace Lumper.UI.ViewModels.Matchers;

using Lumper.UI.Models.Matchers;

public class GlobMatcherViewModel : MatcherViewModel
{
    public override string Name => "Glob";

    public override Matcher ConstructMatcher(string pattern) =>
        new GlobMatcher($"*{pattern}*", string.IsNullOrEmpty(pattern), true);
}
