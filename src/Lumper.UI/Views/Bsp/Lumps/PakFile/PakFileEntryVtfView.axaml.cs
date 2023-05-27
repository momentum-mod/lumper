using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

namespace Lumper.UI.Views.Bsp.Lumps.PakFile;

public partial class PakFileEntryVtfView : UserControl
{
    public PakFileEntryVtfView()
    {
        InitializeComponent();
        if (Application.Current?.ApplicationLifetime is not
            IClassicDesktopStyleApplicationLifetime
            desktop)
            throw new InvalidCastException(
                nameof(Application.Current.ApplicationLifetime));

        Desktop = desktop;
    }

    public IClassicDesktopStyleApplicationLifetime Desktop
    {
        get;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async void OnSetImageClick(object sender, RoutedEventArgs e)
    {
        if (Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions();
        dialog.AllowMultiple = false;
        dialog.Title = "Pick tasks file";
        dialog.FileTypeFilter = GenerateImageFileFilter();
        var result = await Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(dialog);
        if (result is not { Count: 1 })
            return;
        if (DataContext is PakFileEntryVtfViewModel vm)
        {
            vm.SetImage(
             PakFileEntryVtfViewModel.ImageFromFileStream(
                await result[0].OpenReadAsync()));
        }
    }
    private static IReadOnlyList<FilePickerFileType> GenerateImageFileFilter()
    {
        var jsonFilter = new FilePickerFileType("JSON files");
        //todo test and and all the supported formats
        jsonFilter.Patterns = new[] { "*.bmp", "*.jpeg", "*.jpg", "*.png" };
        var anyFilter = new FilePickerFileType("All files");
        anyFilter.Patterns = new[] { "*" };

        return new[] { jsonFilter, jsonFilter };
    }
}
