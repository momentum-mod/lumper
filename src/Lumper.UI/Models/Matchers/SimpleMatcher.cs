using System.Threading.Tasks;

namespace Lumper.UI.Models.Matchers;

public class SimpleMatcher : Matcher
{
    public SimpleMatcher(string pattern) : base(pattern)
    {
    }

    public override async ValueTask<bool> Match(string value)
    {
        return _pattern.Contains(value.Trim());
    }
}