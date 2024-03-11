using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Lumper.UI.ViewModels;
using Lumper.UI.Views;
using Microsoft.Extensions.Logging;

namespace Lumper.UI;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
            desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

        base.OnFrameworkInitializationCompleted();

        LumperLoggerFactory.GetInstance().CreateLogger(GetType()).LogInformation("Application Initialized");
    }
}
