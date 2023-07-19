using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Lumper.UI.ViewModels.Bsp.Lumps.Entity;

namespace Lumper.UI.Views.Bsp.Lumps.Entity;

public partial class EntityLumpView : UserControl
{
    public EntityLumpView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void OnLostFocus(object? sender, RoutedEventArgs args)
    {
        if (DataContext is EntityLumpViewModel vm)
        {
            vm.Save();
        }
    }
}
