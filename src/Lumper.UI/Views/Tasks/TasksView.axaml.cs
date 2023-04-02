using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Lumper.UI.ViewModels.Tasks;

namespace Lumper.UI.Views.Tasks;

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
            throw new InvalidCastException(
                nameof(Application.Current.ApplicationLifetime));

        Desktop = desktop;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private static IReadOnlyList<FilePickerFileType> GenerateJsonFileFilter()
    {
        var jsonFilter = new FilePickerFileType("JSON files");
        jsonFilter.Patterns = new[] { "*.json" };
        var anyFilter = new FilePickerFileType("All files");
        anyFilter.Patterns = new[] { "*" };

        return new[] { jsonFilter, jsonFilter };
    }

    public async void OnLoadClick(object sender, RoutedEventArgs e)
    {
        if (Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions();
        dialog.AllowMultiple = false;
        dialog.Title = "Pick tasks file";
        dialog.FileTypeFilter = GenerateJsonFileFilter();
        var result = await Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(dialog);
        if (result is not { Count: 1 })
            return;
        if (DataContext is TasksViewModel vm)
            vm.Load(await result[0].OpenReadAsync());
    }
    public async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (Desktop.MainWindow is null)
            return;
        var dialog = new FilePickerSaveOptions();
        dialog.Title = "Pick tasks file";
        dialog.FileTypeChoices = GenerateJsonFileFilter();
        var result = await Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
        if (result is null)
            return;
        if (DataContext is TasksViewModel vm)
            vm.Save(await result.OpenWriteAsync());
    }
}
