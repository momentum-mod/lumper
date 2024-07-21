namespace Lumper.UI.Views;

using System;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Services;
using ViewModels;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Stupid shit to get this to behave like a radiobutton. I miss Angular
            PageService.Instance
                .WhenAnyValue(x => x.ActivePage)
                .Subscribe(pageName =>
                {
                    foreach (Control control in PageButtons.Children.Where(child => child is ToggleButton))
                    {
                        var button = (ToggleButton)control;
                        button.IsChecked = pageName is not null &&
                                           button.CommandParameter is not null &&
                                           pageName == (Page)button.CommandParameter;
                    }
                }).DisposeWith(disposables);

            PageService.Instance.ViewPage(Page.EntityEditor);
        });
    }

    private async void Window_OnClosing(object? _, WindowClosingEventArgs e)
    {
        if (ViewModel is null)
            return;

        await MainWindowViewModel.OnClose(e);
    }

    private void PageButton_OnClick(object? _, RoutedEventArgs e)
    {
        // Hack to make this behave like a radio button; don't allow unchecking
        var source = (ToggleButton?)e.Source;
        if (source is not null && !source.IsChecked!.Value)
            source.IsChecked = true;
    }
}
