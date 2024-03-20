namespace Lumper.UI.Views.Tasks;
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Lumper.UI.ViewModels.Tasks;

public partial class TasksView : UserControl
{
    public IClassicDesktopStyleApplicationLifetime Desktop
    {
        get;
    }

    public TasksView()
    {
        InitializeComponent();

        if (Application.Current?.ApplicationLifetime is not
            IClassicDesktopStyleApplicationLifetime
            desktop)
        {
            throw new InvalidCastException(
                nameof(Application.Current.ApplicationLifetime));
        }

        Desktop = desktop;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private static IReadOnlyList<FilePickerFileType> GenerateJsonFileFilter()
    {
        var jsonFilter = new FilePickerFileType("JSON files")
        {
            Patterns = new[] { "*.json" }
        };
        var anyFilter = new FilePickerFileType("All files")
        {
            Patterns = new[] { "*" }
        };

        return new[] { jsonFilter, jsonFilter };
    }

    public async void OnLoadClick(object sender, RoutedEventArgs e)
    {
        if (Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Pick tasks file",
            FileTypeFilter = GenerateJsonFileFilter()
        };
        IReadOnlyList<IStorageFile> result = await Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(dialog);
        if (result is not { Count: 1 })
            return;
        if (DataContext is TasksViewModel vm)
            vm.Load(await result[0].OpenReadAsync());
    }
    public async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerSaveOptions
        {
            Title = "Pick tasks file",
            FileTypeChoices = GenerateJsonFileFilter()
        };
        IStorageFile? result = await Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);

        if (result is null)
            return;

        if (DataContext is TasksViewModel vm)
            vm.Save(await result.OpenWriteAsync());
    }
}
