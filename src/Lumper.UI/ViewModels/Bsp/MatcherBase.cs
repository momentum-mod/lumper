using Lumper.UI.Models;

namespace Lumper.UI.ViewModels.Bsp;

/// <summary>
///     ViewModel base for all <see cref="Matcher" /> instances.
/// </summary>
public abstract class MatcherBase : ViewModelBase
{
    public abstract string Name
    {
        get;
    }

    public abstract Matcher ConstructMatcher(string pattern);
}
