namespace Lumper.UI.ViewModels.Matchers;

using Lumper.UI.Models.Matchers;

public class GlobMatcherViewModelStrictViewModel : MatcherViewModel
{
    public override string Name => "Glob (Strict)";

    public override Matcher ConstructMatcher(string pattern)
        => new GlobMatcher($"{pattern}", string.IsNullOrEmpty(pattern));
}
