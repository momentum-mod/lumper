namespace Lumper.UI.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Lumper.UI.Services;
using Lumper.UI.ViewModels;
using ReactiveUI;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            AddHandler(DragDrop.DragOverEvent, Window_OnDragOver);
            AddHandler(DragDrop.DropEvent, Window_OnDrop);

            // Stupid shit to get this to behave like a radiobutton. I miss Angular
            PageService
                .Instance.WhenAnyValue(x => x.ActivePage)
                .ObserveOn(RxApp.MainThreadScheduler)
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

            PopulateRecentFilesList();
            StateService.Instance.RecentFiles.CollectionChanged += (_, _) => PopulateRecentFilesList();
        });
    }

    private void PopulateRecentFilesList() =>
        RecentFiles.ItemsSource = StateService
            .Instance.RecentFiles.Select(path =>
            {
                const int max = 50;
                string header = path.Length > max ? "..." + path[^max..] : path;

                // Underscore needed otherwise accelerate key thing eats first underscore
                header = "_" + header;

                return new MenuItem
                {
                    Header = header,
                    Command = ReactiveCommand.CreateFromTask(async () => await BspService.Instance.Load(path)),
                };
            })
            .Reverse()
            .ToList();

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

    private async void Window_OnDrop(object? _, DragEventArgs e)
    {
        List<IStorageFile> files = FilterDraggedBspFiles(e);
        await ViewModel!.OpenDragDropped(files);
    }

    private void Window_OnDragOver(object? _, DragEventArgs e)
    {
        bool hasBsps = FilterDraggedBspFiles(e).Count > 0;
        e.DragEffects = hasBsps ? DragDropEffects.Link : DragDropEffects.None;
    }

    private List<IStorageFile> FilterDraggedBspFiles(DragEventArgs e) =>
        e.Data.GetFiles()
            ?.OfType<IStorageFile>()
            .Where(file => file.Name.EndsWith(".bsp", StringComparison.Ordinal))
            .ToList() ?? [];
}
