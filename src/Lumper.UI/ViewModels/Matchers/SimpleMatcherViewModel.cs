namespace Lumper.UI.ViewModels.Matchers;
using Lumper.UI.Models;
using Lumper.UI.Models.Matchers;
using Lumper.UI.ViewModels.Bsp;

/// <summary>
///     ViewModel for <see cref="SimpleMatcher" />.
/// </summary>
public class SimpleMatcherViewModel : MatcherBase
{
    public override string Name => "Simple";

    public override Matcher ConstructMatcher(string pattern) => new SimpleMatcher(pattern, string.IsNullOrEmpty(pattern));
}
