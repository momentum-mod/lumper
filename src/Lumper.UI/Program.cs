namespace Lumper.UI;

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.ReactiveUI;
using NLog;
using ReactiveUI;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // NLog isn't loading nlog.config unless I have this here. No idea why!
        Logger.Info("Program started");

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            RxApp.DefaultExceptionHandler = new ObservableExceptionHandler();

            Desktop.ShutdownRequested += (_, _) => LogManager.Shutdown();
        }
        catch (Exception e)
        {
            if (Debugger.IsAttached)
                Debugger.Break();

            LogManager.GetCurrentClassLogger().Fatal(e);
        }
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .UseReactiveUI();
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
}
