namespace Lumper.UI.Views.Pages.VtfBrowser;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ViewModels.Pages.VtfBrowser;
using ViewModels.Shared.Pakfile;

public partial class VtfBrowserView : ReactiveUserControl<VtfBrowserViewModel>
{
    public VtfBrowserView() => InitializeComponent();

    private void Item_DoubleTapped(object? sender, TappedEventArgs _)
    {
        var selectedVtf = ((Border)sender!).DataContext! as PakfileEntryVtfViewModel;
        var window = new VtfImageWindow { DataContext = selectedVtf, Height = 1024, Width = 1288 }; // 1024 + 256 + 8
        window.Show();
    }
}
