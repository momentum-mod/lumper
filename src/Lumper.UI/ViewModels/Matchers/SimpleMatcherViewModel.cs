namespace Lumper.UI.ViewModels.Matchers;

using Lumper.UI.Models.Matchers;

public class SimpleMatcherViewModel : MatcherViewModel
{
    public override string Name => "Simple";

    public override Matcher ConstructMatcher(string pattern) =>
        new SimpleMatcher(pattern, string.IsNullOrEmpty(pattern));
}
