namespace Lumper.UI.Views;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Lumper.UI.ViewModels;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow() => InitializeComponent();

    private async void Window_OnClosing(object? _, WindowClosingEventArgs e)
    {
        if (ViewModel is null)
            return;

        await ViewModel.OnClose(e);
    }
}
