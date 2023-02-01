using Lumper.UI.Models;
using Lumper.UI.Models.Matchers;
using Lumper.UI.ViewModels.Bsp;

namespace Lumper.UI.ViewModels.Matchers;

/// <summary>
///     ViewModel for <see cref="GlobMatcher" /> without start and end checks.
/// </summary>
public class GlobMatcherViewModel : MatcherBase
{
    public override string Name => "Glob";

    public override Matcher ConstructMatcher(string pattern)
    {
        return new GlobMatcher($"*{pattern}*", true);
    }
}
