namespace Lumper.UI.ViewModels;
using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Lumper.UI.ViewModels.LogViewer;
using NLog;
using ReactiveUI;
using Services;

/// <summary>
///     ViewModel for MainWindow. Handles File, View menus etc., and page loading
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private IClassicDesktopStyleApplicationLifetime Desktop { get; }

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public LogViewerViewModel LogViewer { get; set; } = new();

    public MainWindowViewModel()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidCastException(nameof(Application.Current.ApplicationLifetime));

        Desktop = desktop;

        LoggingInit();
        PagesInit();

        if (Desktop.Args is { Length: 1 })
        {
            // This is an async method but we want it on this thread, just use the scheduler.
            Observable.Start(
                () => ActiveBspService.Instance.Load(Desktop.Args[0]),
                RxApp.MainThreadScheduler);
        }
    }

    private void LoggingInit()
    {
        LogManager
            .Setup()
            .LoadConfiguration(builder =>
            {
                builder.ForLogger().WriteToFile(fileName: "logs.txt");
                builder
                    .ForLogger()
                    .WriteToMethodCall((logEvent, _) => LogViewer.AddLog(logEvent));
#if DEBUG
                builder.ForLogger().WriteToConsole();
#endif
            });
    }
}
