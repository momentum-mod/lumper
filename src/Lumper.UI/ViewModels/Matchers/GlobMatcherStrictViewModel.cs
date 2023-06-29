using Lumper.UI.Models;
using Lumper.UI.Models.Matchers;
using Lumper.UI.ViewModels.Bsp;

namespace Lumper.UI.ViewModels.Matchers;

/// <summary>
///     ViewModel for <see cref="GlobMatcher" />.
/// </summary>
public class GlobMatcherStrictViewModel : MatcherBase
{
    public override string Name => "Glob (Strict)";

    public override Matcher ConstructMatcher(string pattern)
    {
        return new GlobMatcher($"{pattern}", string.IsNullOrEmpty(pattern));
    }
}
