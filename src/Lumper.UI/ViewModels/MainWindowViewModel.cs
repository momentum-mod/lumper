using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Lumper.UI.ViewModels.Bsp;
using ReactiveUI;

namespace Lumper.UI.ViewModels;

/// <summary>
///     ViewModel for MainWindow
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private BspViewModel? _bspModel;
    private BspNodeBase? _selectedNode;

    public MainWindowViewModel()
    {
        if (Application.Current?.ApplicationLifetime is not
            IClassicDesktopStyleApplicationLifetime
            desktop)
            throw new InvalidCastException(
                nameof(Application.Current.ApplicationLifetime));

        Desktop = desktop;

        SearchInit();
        TabsInit();
        IOInit();

        if (desktop.Args?.Length > 0)
        {
            LoadBsp(desktop.Args[0]);
        }
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

    internal static void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Link;

        var names = e.Data.GetFileNames() ?? new List<string>();

        if (!e.Data.Contains(DataFormats.FileNames) || !names.FirstOrDefault("").ToLower().EndsWith(".bsp"))
            e.DragEffects = DragDropEffects.None;
    }

    internal void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.FileNames))
            return;

        var names = e.Data.GetFileNames() ?? new List<string>();
        foreach (string target in names)
        {
            if (!target.ToLower().EndsWith(".bsp"))
                continue;

            if (BspModel == null)
            {
                // if nothing is open, open it.
                LoadBsp(target);
            }
            else
            {
                // Otherwise, open a brand new Lumper instance with it.
                string? executableFileName = Process.GetCurrentProcess().MainModule?.FileName;
                if (executableFileName == null)
                    return;

                ProcessStartInfo startInfo = new()
                {
                    ArgumentList = { $"{target}" },
                    FileName = executableFileName,
                };

                Process.Start(startInfo);
            }
        }
    }
}
