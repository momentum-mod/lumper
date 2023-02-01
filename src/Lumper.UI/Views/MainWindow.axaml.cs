using System;
using System.ComponentModel;
using Avalonia.Controls;
using Lumper.UI.ViewModels;

namespace Lumper.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel model)
            throw new ArgumentOutOfRangeException();
        await model.OnClose(e);
    }
}
