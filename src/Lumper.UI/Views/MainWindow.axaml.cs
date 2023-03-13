using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Lumper.UI.ViewModels;

namespace Lumper.UI.Views;

public partial class MainWindow : Window
{
    // Quietly shadow DataContext to add initializer
    public new object? DataContext
    {
        init // DataContext is never overwritten-- It's ok to not defensively remove the Drop handler, and do DragOver here.
        {
            var dc = value as MainWindowViewModel; // It must be (as of 3/16/23)
            AddHandler(DragDrop.DropEvent, dc!.Drop);
            AddHandler(DragDrop.DragOverEvent, MainWindowViewModel.DragOver);

            base.DataContext = value;
        }

        get => base.DataContext;
    }

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
