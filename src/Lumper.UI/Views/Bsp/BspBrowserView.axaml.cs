using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Lumper.UI.Views.Bsp;

public partial class BspBrowserView : UserControl
{
    public BspBrowserView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}