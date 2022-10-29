using System.Threading.Tasks;

namespace Lumper.UI.Models;

public abstract class Matcher
{
    protected readonly string _pattern;

    public Matcher(string pattern)
    {
        _pattern = pattern;
    }

    public abstract ValueTask<bool> Match(string value);
}