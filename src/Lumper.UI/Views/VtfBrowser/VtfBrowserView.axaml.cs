namespace Lumper.UI.Views.VtfBrowser;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Lumper.UI.ViewModels.VtfBrowser;

public partial class VtfBrowserView : ReactiveUserControl<VtfBrowserViewModel>
{
    public VtfBrowserView() => InitializeComponent();

    private void Item_DoubleTapped(object? sender, TappedEventArgs _)
    {
        var selectedVtf = (VtfBrowserItemViewModel)((Border)sender!).DataContext!;
        var window = new VtfImageWindow { DataContext = selectedVtf };
        window.Show();
    }
}
