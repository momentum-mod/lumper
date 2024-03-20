namespace Lumper.UI.ViewModels.Matchers;
using Lumper.UI.Models;
using Lumper.UI.Models.Matchers;
using Lumper.UI.ViewModels.Bsp;

/// <summary>
///     ViewModel for <see cref="GlobMatcher" />.
/// </summary>
public class GlobMatcherStrictViewModel : MatcherBase
{
    public override string Name => "Glob (Strict)";

    public override Matcher ConstructMatcher(string pattern) => new GlobMatcher($"{pattern}", string.IsNullOrEmpty(pattern));
}
