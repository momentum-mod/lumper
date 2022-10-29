using System.Threading.Tasks;
using GlobExpressions;

namespace Lumper.UI.Models.Matchers;

public class GlobMatcher : Matcher
{
    private readonly Glob _matcher;

    public GlobMatcher(string pattern, bool ignoreCase = false) : base(pattern)
    {
        _matcher = new Glob(pattern,
            GlobOptions.Compiled | (ignoreCase ? GlobOptions.CaseInsensitive : GlobOptions.None));
    }

    public override async ValueTask<bool> Match(string value)
    {
        return _matcher.IsMatch(value.Trim());
    }
}