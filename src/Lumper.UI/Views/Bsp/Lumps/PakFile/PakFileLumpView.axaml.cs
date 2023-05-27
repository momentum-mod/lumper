using System;
using System.IO;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

namespace Lumper.UI.Views.Bsp.Lumps.PakFile;

public partial class PakFileLumpView : UserControl
{
    public PakFileLumpView()
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

    public async void OnImportClick(object sender, RoutedEventArgs e)
    {
        //todo redundant code import/export
        if (Desktop.MainWindow is null)
            return;
        var dialog = new FolderPickerOpenOptions();
        dialog.Title = "PakFile import directory";
        var result = await Desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(dialog);
        if (result is null)
            return;
        Uri uri;
        if (DataContext is PakFileLumpViewModel vm
            && result[0].TryGetUri(out uri))
        {
            vm.Import(uri.AbsolutePath);
        }
    }
    public async void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (Desktop.MainWindow is null)
            return;
        var dialog = new FolderPickerOpenOptions();
        dialog.Title = "PakFile export directory";
        var result = await Desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(dialog);
        if (result is null)
            return;
        Uri uri;
        if (DataContext is PakFileLumpViewModel vm
            && result[0].TryGetUri(out uri))
        {
            vm.Export(uri.AbsolutePath);
        }
    }
}
