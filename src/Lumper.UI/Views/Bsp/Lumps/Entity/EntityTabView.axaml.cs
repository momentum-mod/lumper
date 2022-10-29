using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Lumper.UI.Views.Bsp.Lumps.Entity;

public partial class EntityTabView : UserControl
{
    public EntityTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}