namespace Lumper.UI.Views;

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Lumper.UI.Services;
using Lumper.UI.ViewModels;
using Material.Icons;
using ReactiveUI;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Stupid shit to get this to behave like a radiobutton. I miss Angular
            PageService
                .Instance.WhenAnyValue(x => x.ActivePage)
                .Subscribe(pageName =>
                {
                    foreach (Control control in PageButtons.Children.Where(child => child is ToggleButton))
                    {
                        var button = (ToggleButton)control;
                        button.IsChecked =
                            pageName is not null
                            && button.CommandParameter is not null
                            && pageName == (Page)button.CommandParameter;
                    }
                })
                .DisposeWith(disposables);

            PageService.Instance.ViewPage(Page.EntityEditor);

            StateService.Instance.RecentFiles.CollectionChanged += (_, _) =>
                RecentFiles.ItemsSource = StateService
                    .Instance.RecentFiles.Select(path =>
                    {
                        const int max = 40;
                        if (path.Length > max)
                            path = "..." + path[^max..];
                        // Underscore needed otherwise accelerate key thing eats first underscore
                        path = "_" + path;

                        return new MenuItem
                        {
                            Header = path,
                            Command = ReactiveCommand.CreateFromTask(async () => await BspService.Instance.Load(path)),
                        };
                    })
                    .Reverse()
                    .ToList();

            GameSyncService
                .Instance.WhenAnyValue(x => x.Status)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(status =>
                {
                    SyncButton.IsEnabled =
                        status
                            is not (GameSyncService.SyncStatus.Connecting or GameSyncService.SyncStatus.Disconnecting);

                    SyncIcon.Foreground =
                        status is GameSyncService.SyncStatus.Connected ? Brushes.LawnGreen : Brushes.LightGray;

                    SyncText.Text = status switch
                    {
                        GameSyncService.SyncStatus.Connected => "Connected to Game Sync",
                        GameSyncService.SyncStatus.Disconnected => "Connect to Game Sync",
                        GameSyncService.SyncStatus.Connecting => "Connecting...",
                        GameSyncService.SyncStatus.Disconnecting => "Disconnecting...",
                        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
                    };
                })
                .DisposeWith(disposables);
        });
    }

    // ReSharper disable once AsyncVoidMethod - Needed for event binding
    private async void Window_OnClosing(object? _, WindowClosingEventArgs e)
    {
        if (ViewModel is null)
            return;

        await MainWindowViewModel.OnClose(e);
    }

    private void PageButton_OnClick(object? _, RoutedEventArgs e)
    {
        // Hack to make this behave like a radio button; don't allow unchecking
        var source = (ToggleButton?)e.Source;
        if (source is not null && !source.IsChecked!.Value)
            source.IsChecked = true;
    }
}
