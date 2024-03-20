namespace Lumper.UI.Views.Bsp.Lumps.PakFile;
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

public partial class PakFileEntryVtfView : UserControl
{
    public PakFileEntryVtfView()
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

    public IClassicDesktopStyleApplicationLifetime Desktop
    {
        get;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void OnSetImageClick(object sender, RoutedEventArgs e) => SetImage(false);
    public void OnNewImageClick(object sender, RoutedEventArgs e) => SetImage(true);
    private async void SetImage(bool createNew)
    {
        if (Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Pick tasks file",
            FileTypeFilter = GenerateImageFileFilter()
        };

        IReadOnlyList<IStorageFile> result = await Desktop.MainWindow.StorageProvider
            .OpenFilePickerAsync(dialog);

        if (result is not { Count: 1 })
            return;

        if (DataContext is PakFileEntryVtfViewModel vm)
        {
            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> img = PakFileEntryVtfViewModel.ImageFromFileStream(
                await result[0].OpenReadAsync());
            if (createNew)
                vm.SetNewImage(img);
            else
                vm.SetImageData(img);
        }
    }
    private static IReadOnlyList<FilePickerFileType> GenerateImageFileFilter()
    {
        var imageFilter = new FilePickerFileType("Image files")
        {
            Patterns = new[] { "*.bmp", "*.jpeg", "*.jpg", "*.png" }
        };
        var anyFilter = new FilePickerFileType("All files")
        {
            Patterns = new[] { "*" }
        };
        return new[] { imageFilter, anyFilter
};
    }
}
