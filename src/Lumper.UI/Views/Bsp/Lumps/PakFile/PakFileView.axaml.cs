using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Lumper.UI.Views.Bsp.Lumps.PakFile;

public partial class PakFileView : UserControl
{
    public PakFileView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
