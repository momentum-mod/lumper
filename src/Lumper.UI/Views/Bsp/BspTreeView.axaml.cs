using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Lumper.UI.ViewModels.Bsp;

namespace Lumper.UI.Views.Bsp;

public partial class BspTreeView : UserControl
{
    public BspTreeView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                OpenSelectedNode();
                break;
            case Key.Delete:
                //todo
                break;

        }
    }

    public void OnClick(object sender, TappedEventArgs e)
    {
        OpenSelectedNode();
    }

    private void OpenSelectedNode()
    {
        if (DataContext is not BspViewModel model)
        {
            throw new NotSupportedException();
        }
        model.Open(model.SelectedNode);
    }
}
