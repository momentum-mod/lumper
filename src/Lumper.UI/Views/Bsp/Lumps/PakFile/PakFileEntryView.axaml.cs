using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Lumper.UI.Views.Bsp.Lumps.PakFile;

public partial class PakFileEntryView : UserControl
{
    public PakFileEntryView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

}
