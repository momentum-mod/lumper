using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Lumper.UI.ViewModels.VtfBrowser;

namespace Lumper.UI.Views.VtfBrowser;

public partial class VtfBrowserView : UserControl
{
    public VtfBrowserView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Item_DoubleTapped(object? sender, TappedEventArgs e)
    {
        var selectedVtf = (VtfBrowserItemViewModel)((Border)sender!).DataContext!;

        var window = new VtfImageWindow();
        window.DataContext = selectedVtf;
        window.Show();
    }
}
