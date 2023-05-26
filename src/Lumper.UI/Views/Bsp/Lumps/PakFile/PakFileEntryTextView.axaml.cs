using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Lumper.UI.Views.Bsp.Lumps.PakFile;

public partial class PakFileEntryTextView : UserControl
{
    public PakFileEntryTextView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
