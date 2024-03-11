using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace Lumper.UI.ViewModels;

/// <summary>
///     Base for all ViewModels
/// </summary>
public class ViewModelBase : ReactiveObject
{
    protected ILogger _logger;

    public ViewModelBase()
    {
        _logger = LumperLoggerFactory.GetInstance().CreateLogger(GetType());
    }
}
