using System;
using System.Threading.Tasks;
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

    private async ValueTask<string?> PickFolder(string title)
    {
        string path = null;
        if (Desktop.MainWindow is null)
            return path;
        var dialog = new FolderPickerOpenOptions()
        {
            Title = title
        };
        var result = await Desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(dialog);

        if (result is not { Count: 1 })
            return path;

        if (result[0].TryGetUri(out var uri))
        {
            path = uri.AbsolutePath;
        }
        return path;
    }

    public async void OnImportClick(object sender, RoutedEventArgs e)
    {
        string? path;
        if (DataContext is PakFileLumpViewModel vm
            && (path = await PickFolder("PakFile import directory")) is not null)
        {
            vm.Import(path);
        }
    }
    public async void OnExportClick(object sender, RoutedEventArgs e)
    {
        string? path;
        if (DataContext is PakFileLumpViewModel vm
            && (path = await PickFolder("PakFile export directory")) is not null)
        {
            vm.Export(path);
        }
    }
}
