using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

namespace Lumper.UI.Views.Bsp.Lumps.PakFile;

public partial class PakFileEntryBranchView : UserControl
{
    public PakFileEntryBranchView()
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

    public async void OnAddFileClick(object sender, RoutedEventArgs e)
    {
        if (Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions()
        {
            AllowMultiple = true,
            Title = "Pick files to add to PakFile"
        };
        var result = await Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(dialog);
        if (DataContext is PakFileEntryBranchViewModel vm)
        {
            foreach (var r in result)
                vm.AddFile(r.Name, await r.OpenReadAsync());
        }
    }

    public void OnAddDirClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is PakFileEntryBranchViewModel vm)
        {
            vm.AddDir("new_dir");
        }
    }
}
