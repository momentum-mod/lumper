namespace Lumper.UI.Models.Matchers;

/// <summary>
/// Base class for pattern matching
/// </summary>
public abstract class Matcher(string pattern, bool isEmpty)
{
    protected string Pattern { get; } = pattern;

    public bool IsEmpty { get; } = isEmpty;

    public abstract bool Match(string? value);
}
