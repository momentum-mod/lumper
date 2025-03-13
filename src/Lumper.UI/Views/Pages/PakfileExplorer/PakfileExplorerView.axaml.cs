namespace Lumper.UI.Views.Pages.PakfileExplorer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Lumper.UI.ViewModels.Pages.PakfileExplorer;
using NLog;
using ReactiveUI;

public partial class PakfileExplorerView : ReactiveUserControl<PakfileExplorerViewModel>
{
    public PakfileExplorerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            ViewModel!
                .WhenAnyValue(x => x.DataGridSource)
                .Where(x => x is not null)
                .Subscribe(grid =>
                {
                    // Ugly as hell but very hard to make this work.
                    IObservable<
                        EventPattern<TreeSelectionModelSelectionChangedEventArgs<PakfileTreeNodeViewModel>>
                    > selectionEventObs = Observable.FromEventPattern<
                        TreeSelectionModelSelectionChangedEventArgs<PakfileTreeNodeViewModel>
                    >(h => grid!.RowSelection!.SelectionChanged += h, h => grid!.RowSelection!.SelectionChanged -= h);

                    IObservable<bool> isDirectory = selectionEventObs.Select(x =>
                        (IReadOnlyList<PakfileTreeNodeViewModel>)x.EventArgs.SelectedItems is [{ IsDirectory: true }]
                    );

                    Grid.ContextMenu = new ContextMenu
                    {
                        ItemsSource = new List<object>
                        {
                            new MenuItem
                            {
                                Header = "Import File(s)",
                                [!IsVisibleProperty] = isDirectory.ToBinding(),
                                Command = ReactiveCommand.CreateFromTask(() => ViewModel!.ImportFiles()),
                            },
                            new MenuItem
                            {
                                Header = "Import Directory",
                                [!IsVisibleProperty] = isDirectory.ToBinding(),
                                Command = ReactiveCommand.CreateFromTask(() => ViewModel!.ImportDirectory()),
                            },
                            new MenuItem
                            {
                                Header = "Create File",
                                [!IsVisibleProperty] = isDirectory.ToBinding(),
                                Command = ReactiveCommand.CreateFromTask(() => ViewModel!.CreateEmptyFile()),
                            },
                            new MenuItem
                            {
                                Header = "Create Directory",
                                [!IsVisibleProperty] = isDirectory.ToBinding(),
                                Command = ReactiveCommand.CreateFromTask(() => ViewModel!.CreateEmptyDirectory()),
                            },
                            new Separator { [!IsVisibleProperty] = isDirectory.ToBinding() },
                            new MenuItem
                            {
                                Header = "Export",
                                Command = ReactiveCommand.CreateFromTask(() => ViewModel!.ExportFiles()),
                            },
                            new Separator(),
                            new MenuItem
                            {
                                Header = "Rename",
                                Command = ReactiveCommand.CreateFromTask(() => ViewModel!.RenameSelected()),
                            },
                            new MenuItem
                            {
                                Header = "Delete",
                                Command = ReactiveCommand.Create(() => ViewModel!.DeleteSelected()),
                            },
                        },
                    };
                })
                .DisposeWith(disposables)
        );
    }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private void Grid_OnRowDragOver(object? _, TreeDataGridRowDragEventArgs e)
    {
        if (!CanDragTo(e.TargetRow.Model, e))
            e.Inner.DragEffects = DragDropEffects.None;
    }

    private void Grid_OnRowDrop(object? _, TreeDataGridRowDragEventArgs e)
    {
        if (Selected is null)
        {
            e.Inner.DragEffects = DragDropEffects.None;
            return;
        }

        if (!CanDragTo(e.TargetRow.Model, e) || Grid?.Rows is null)
        {
            e.Handled = true;
            e.Inner.DragEffects = DragDropEffects.None;
            return;
        }

        // Schedule update stuff on UI thread after tree has definitely finished updating
        // (see https://github.com/AvaloniaUI/Avalonia.Controls.TreeDataGrid/pull/199#issuecomment-1674131384)
        Dispatcher.UIThread.Post(() => ViewModel!.OnMoveFiles(), DispatcherPriority.Background);
    }

    private List<PakfileTreeNodeViewModel>? Selected =>
        Grid.RowSelection?.SelectedItems.Cast<PakfileTreeNodeViewModel>().ToList();

    private bool CanDragTo(object? node, TreeDataGridRowDragEventArgs e)
    {
        if (e.TargetRow.Model is null)
            return false;

        // Only allow dragging into directories - the After/Before stuff is buggy as shit, ugh
        if (e.Position != TreeDataGridRowDropPosition.Inside || node is PakfileTreeNodeViewModel { IsDirectory: false })
            return false;

        List<PakfileTreeNodeViewModel>? dragItems = Selected;
        if (dragItems is null)
            return false;

        // Only allow drag-drop is all the selected items are within the same directory.
        // Drag-drop is very temperamental, and we can't catch any errors it throws. I find moving big stacks of
        // directories around can easily throw. Frustratingly, we can't catch errors thrown by this control, so they'll
        // crash the application. So this is a very conservative way to try to stop this thing from exploding.
        if (dragItems.Count > 1 && dragItems.Any(x => x.Parent != dragItems[0].Parent))
        {
            Logger.Warn("Dragging items from multiple directories is not supported, it keeps crashing!! Sorry!");
            return false;
        }

        return true;
    }
}
