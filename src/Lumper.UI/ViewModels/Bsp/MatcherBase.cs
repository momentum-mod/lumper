using Lumper.UI.Models;

namespace Lumper.UI.ViewModels.Bsp;

public abstract class MatcherBase : ViewModelBase
{
    public abstract string Name { get; }
    public abstract Matcher ConstructMatcher(string pattern);
}