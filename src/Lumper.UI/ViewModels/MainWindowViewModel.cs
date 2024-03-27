namespace Lumper.UI.ViewModels;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;

/// <summary>
///     ViewModel for MainWindow. Handles File, View menus etc., and page loading
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{

    public MainWindowViewModel()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            throw new InvalidCastException(nameof(Application.Current.ApplicationLifetime));

        Desktop = desktop;

        PagesInit();
        IOInit();
    }

    {
    }

    private void OnInitialized()
    {
        if (Desktop.Args is { Length: 1 })
        {
            // This is an async method but we want it on this thread, just use the scheduler.
            Observable.Start(
                () => ActiveBspService.Instance.Load(Desktop.Args[0]),
                RxApp.MainThreadScheduler);
        }
    }
}
