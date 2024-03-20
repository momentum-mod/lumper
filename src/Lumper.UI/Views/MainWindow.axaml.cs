namespace Lumper.UI.Views;
using System;
using Avalonia.Controls;
using Lumper.UI.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not MainWindowViewModel model)
            throw new ArgumentOutOfRangeException();
        await model.OnClose(e);
    }
}
