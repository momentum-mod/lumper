namespace Lumper.UI.ViewModels;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;

/// <summary>
///     ViewModel for MainWindow
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

    {
    }
}
