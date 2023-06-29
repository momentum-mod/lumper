using Lumper.UI.Models;
using Lumper.UI.Models.Matchers;
using Lumper.UI.ViewModels.Bsp;

namespace Lumper.UI.ViewModels.Matchers;

/// <summary>
///     ViewModel for <see cref="SimpleMatcher" />.
/// </summary>
public class SimpleMatcherViewModel : MatcherBase
{
    public override string Name => "Simple";

    public override Matcher ConstructMatcher(string pattern)
    {
        return new SimpleMatcher(pattern, string.IsNullOrEmpty(pattern));
    }
}
