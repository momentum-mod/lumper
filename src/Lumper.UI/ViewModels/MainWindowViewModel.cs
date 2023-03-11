using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Lumper.UI.ViewModels.Bsp;
using ReactiveUI;

namespace Lumper.UI.ViewModels;

/// <summary>
///     ViewModel for MainWindow
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase? content;
    private BspNodeBase? _selectedNode;
    private BspViewModel? _bspModel;

    public MainWindowViewModel()
    {
        if (Application.Current?.ApplicationLifetime is not
            IClassicDesktopStyleApplicationLifetime
            desktop)
            throw new InvalidCastException(
                nameof(Application.Current.ApplicationLifetime));

        Desktop = desktop;

        IOInit();
        Content = BspModel;
    }

    private void OnLoad()
    {
        if (Desktop.Args is not { Length: 1 })
            return;
        LoadBsp(Desktop.Args[0]);
    }

    public ViewModelBase? Content
    {
        get => content;
        private set => this.RaiseAndSetIfChanged(ref content, value);
    }

    public IClassicDesktopStyleApplicationLifetime Desktop
    {
        get;
    }

    public BspViewModel? BspModel
    {
        get => _bspModel;
        set => this.RaiseAndSetIfChanged(ref _bspModel, value);
    }

    public BspNodeBase? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }
}
