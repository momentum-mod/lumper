namespace Lumper.UI.ViewModels.Matchers;

using Models.Matchers;

public abstract class MatcherViewModel : ViewModel
{
    public abstract string Name { get; }

    public abstract Matcher ConstructMatcher(string pattern);
}
