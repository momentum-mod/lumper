namespace Lumper.UI.ViewModels.Pages.PakfileExplorer;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;
using Shared.Pakfile;
using Node = PakfileTreeNodeViewModel;

public sealed class PakfileExplorerViewModel : ViewModel
{
    private PakfileTreeViewModel? Tree { get; set; }

    private PakfileLumpViewModel? _pakfileLump;

    [Reactive]
    public HierarchicalTreeDataGridSource<Node>? DataGridSource { get; set; }

    [Reactive]
    public PakfileEntryViewModel? ActiveFile { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public PakfileExplorerViewModel() => BspService.Instance
        .WhenAnyValue(x => x.PakfileLumpViewModel)
        .Subscribe(pakfile =>
        {
            if (pakfile is null)
            {
                Tree = null;
                DataGridSource = null;
                SetActiveFile(null);
                return;
            }

            _pakfileLump = pakfile;
            Tree = new PakfileTreeViewModel(pakfile.Entries);

            DataGridSource = new HierarchicalTreeDataGridSource<Node>(Tree.Root.Children ?? []) {
                Columns = {
                    new HierarchicalExpanderColumn<Node>(
                        new TemplateColumn<Node>(
                            "Name",
                            "EntryNameCell",
                            null, // No edit cell, doesn't work well, we use dialog window instead
                            new GridLength(4, GridUnitType.Star),
                            new TemplateColumnOptions<Node> {
                                CompareAscending = Node.SortAscending(x => x.Name),
                                CompareDescending = Node.SortDescending(x => x.Name),
                                IsTextSearchEnabled = true,
                                TextSearchValueSelector = x => x.Name,
                                BeginEditGestures = BeginEditGestures.None
                            }),
                        x => x.Children,
                        x => x.IsDirectory,
                        x => x.IsExpanded),
                    new TextColumn<Node, string?>(
                        "Extension",
                        x => x.Extension,
                        new GridLength(1, GridUnitType.Star),
                        options: new TextColumnOptions<Node> {
                            CompareAscending = Node.SortAscending(x => x.Extension),
                            CompareDescending = Node.SortDescending(x => x.Extension)
                        }),
                    new TemplateColumn<Node>(
                        "Size (Compressed)",
                        "EntrySizeCell",
                        null,
                        new GridLength(1, GridUnitType.Star),
                        new TemplateColumnOptions<Node> {
                            CompareAscending = Node.SortAscending(x => x.Size),
                            CompareDescending = Node.SortDescending(x => x.Size),
                            IsTextSearchEnabled = true,
                            TextSearchValueSelector = x => x.Name
                        })
                    // TODO: We could do an extra column for percentage of overall size,
                    // bit of work to recalculate everything as stuff moves so cba rn
                }
            };

            DataGridSource.RowSelection!.SingleSelect = false;
            DataGridSource.RowSelection!.SelectionChanged += (_, _) =>
                SetActiveFile(DataGridSource?.RowSelection.SelectedItems.Count > 0
                    ? DataGridSource?.RowSelection.SelectedItems[0]
                    : null);
        });

    // Null here is fine, just closes RHS pane.
    private void SetActiveFile(Node? node)
    {
        if (node is null)
        {
            ActiveFile = null;
            return;
        }

        if (node.Leaf is null)
            return;

        node.Leaf.Load();
        ActiveFile = node.Leaf;
    }

    public void Unsort() => DataGridSource?.Sort(null);

    public void ExpandAll() => DataGridSource?.ExpandAll();

    public void CollapseAll() => DataGridSource?.CollapseAll();

    public void OnMoveFiles() => Observable.Start(PushTreeChangesToEntries, RxApp.MainThreadScheduler);

    // Stores current treegrid selection in out var returns if it's valid,
    // including if only one is selected if single is true
    private bool GetSelection(out IReadOnlyList<Node> nodes, bool single, bool onlyDirectory = false)
    {
        nodes = DataGridSource?.RowSelection?.SelectedItems!;
        return nodes is { Count: > 0 } &&
               (!single || nodes.Count == 1) &&
               (!onlyDirectory || nodes.All(node => node.IsDirectory)) &&
               Tree is not null;
    }


    private void RemoveRecursive(Node node)
    {
        if (node.Children is not null)
        {
            foreach (Node child in node.Children.ToList())
                RemoveRecursive(child);

            // If this is a directory it doesn't live in the SourceCache,
            // so manually remove from tree.
            node.RemoveSelf();
            node.Parent?.RecalculateSize();
        }
        else
        {
            // This *does* live in SourceCache (which DeleteEntry removes this from),
            // so no need to update tree.
            _pakfileLump!.DeleteEntry(node.Leaf!);
        }
    }

    // Note this doesn't handle deletions, kept in DeleteSelected for now
    private async ValueTask PushTreeChangesToEntries()
    {
        // TODO: size updates?
        if (Tree is null)
            return;

        PakfileTreeViewModel.FixParentsRecursive(Tree.Root);

        var queriedUser = false;
        var moveReferences = false;
        foreach (Node node in Tree.EnumerateLeaves())
        {
            // GetPathString() is computed on call, if parent (directory) name changed,
            // all children will update here.
            var newKey = node.PathString;
            if (newKey == node.Leaf!.Key)
                continue;

            if (!queriedUser)
            {
                ButtonResult result = await MessageBoxManager
                    .GetMessageBoxStandard(
                        "Update references?",
                        "Do you want to update instances of this path in other parts of the BSP?",
                        ButtonEnum.YesNo)
                    .ShowWindowDialogAsync(Program.Desktop.MainWindow);

                moveReferences = result == ButtonResult.Yes;
                queriedUser = true;
            }

            if (moveReferences)
                _pakfileLump!.UpdatePathReferences(newKey, node.Leaf.Key);

            // Update the actual key on the PakFileEntry - this is what causes the move to handle on save.
            node.Leaf.Key = newKey;
            node.Leaf.MarkAsModified();
        }
    }

    public async Task ImportFiles()
    {
        if (!GetSelection(out IReadOnlyList<Node> items, single: true))
            return;

        IReadOnlyList<IStorageFile>? files = await PickFiles();
        if (files is null || _pakfileLump is null)
            return;

        List<string> branchPath = items[0].Path;
        (string, Stream)[] streams = await Task.WhenAll(files.Select(async x => (x.Name, await x.OpenReadAsync())));
        _pakfileLump.Entries.Edit(updater =>
        {
            foreach ((var name, Stream stream) in streams)
            {
                _pakfileLump!.AddEntry(string.Join("/", [.. branchPath, name]), stream, updater);
                stream.Dispose();
            }
        });
    }

    public async Task ImportDirectory()
    {
        if (!GetSelection(out IReadOnlyList<Node> items, single: true, onlyDirectory: true))
            return;
        List<string> branchPath = items[0].Path;

        IStorageFolder? folder = await PickFolder();
        if (folder is null || _pakfileLump is null)
            return;

        var rootPath = folder.Path.LocalPath;
        _pakfileLump.Entries.Edit(updater =>
        {
            foreach (var path in Directory.EnumerateFiles(folder.Path.LocalPath, "*.*", SearchOption.AllDirectories))
            {
                FileStream stream = File.OpenRead(path);
                _pakfileLump!.AddEntry(
                    string.Join("/", [.. branchPath, Path.GetRelativePath(rootPath, path).Replace('\\', '/')]),
                    stream,
                    updater);
                stream.Dispose();
            }
        });
    }

    public async Task CreateEmptyDirectory()
    {
        if (!GetSelection(out IReadOnlyList<Node> items, single: true, onlyDirectory: true))
            return;

        List<string> branchPath = items[0].Path;
        IMsBox<string> msBox = CreateMessageBox("Create Directory", "Directory Name", "Create");
        await ShowMessageBox(msBox);

        var name = msBox.InputValue;
        if (name is null)
            return;

        List<string> pathList = [.. branchPath, name];
        Tree!.Root.AddDirectory(pathList);
        Logger.Info($"Created directory {string.Join("/", pathList)}");
    }

    public async Task CreateEmptyFile()
    {
        if (!GetSelection(out IReadOnlyList<Node> items, single: true, onlyDirectory: true) || _pakfileLump is null)
            return;

        List<string> branchPath = items[0].Path;
        IMsBox<string> msBox = CreateMessageBox("Create File", "File Name", "Create");
        await ShowMessageBox(msBox);

        var name = msBox.InputValue ?? "new file";
        if (name.ToLower().EndsWith(".vtf"))
        {
            Logger.Error("Creating empty VTFs is not supported, please import one.");
            return;
        }

        List<string> pathList = [.. branchPath, name];
        var path = string.Join("/", pathList);

        using var stream = new MemoryStream();
        var entry = (PakfileEntryTextViewModel)_pakfileLump.AddEntry(string.Join("/", path), stream);
        SetActiveFile(Tree!.Find(pathList));
        entry.LoadContent();

        Logger.Info($"Created file {string.Join("/", path)}");
    }


    public async Task RenameSelected()
    {
        if (!GetSelection(out IReadOnlyList<Node> items, single: true))
            return;

        Node item = items[0];
        var type = item.IsDirectory ? "directory" : "file";

        IMsBox<string> msBox = CreateMessageBox($"Rename {type}", "Rename", "Rename", item.Name);

        var result = await ShowMessageBox(msBox);
        if (result == "Cancel")
            return;

        var name = msBox.InputValue;
        if (name is null)
            return;

        // This handles both leaf and branch nodes - PushTreeChangesToEntries does all the work.
        item.Name = name;
        await PushTreeChangesToEntries();
    }

    public void DeleteSelected()
    {
        if (!GetSelection(out IReadOnlyList<Node> items, single: true))
            return;

        foreach (Node node in items.ToList())
            RemoveRecursive(node); // Handles pushing changes to PakfileLumpViewmodel
    }

    public async Task ReplaceContents()
    {
        IStorageFolder? folder = await PickFolder("Replace Pakfile Contents with Directory Contents");
        if (folder is null || _pakfileLump is null)
            return;

        var rootPath = folder.Path.LocalPath;

        var importDir = new DirectoryInfo(rootPath);
        if (!importDir.Exists)
            throw new DirectoryNotFoundException(rootPath);

        _pakfileLump.Entries.Edit(updater =>
        {
            updater.Clear();

            foreach (var path in Directory.EnumerateFiles(importDir.FullName, "*.*", SearchOption.AllDirectories))
            {
                var stream = new MemoryStream();
                new FileStream(path, FileMode.Open).CopyTo(stream);

                var entryPath = Path.GetRelativePath(rootPath, path).Replace('\\', '/');
                _pakfileLump.AddEntry(entryPath, stream, updater);
            }
        });
    }

    public async Task ExportContents()
    {
        IStorageFolder? folder = await PickFolder("Export Pakfile Contents to Directory");
        if (folder is null || _pakfileLump is null)
            return;

        var rootPath = folder.Path.LocalPath;
        var exportDir = new DirectoryInfo(rootPath);
        if (!exportDir.Exists)
            throw new DirectoryNotFoundException(rootPath);

        try
        {
            foreach (PakfileEntryViewModel entry in _pakfileLump.Entries.Items)
            {
                FileInfo fi = new(Path.Join(exportDir.FullName, entry.Key));
                if (fi.Exists)
                {
                    Logger.Warn($"{fi.FullName} already exists, not overwriting.");
                    continue;
                }

                var dirName = fi.Directory?.FullName;
                if (string.IsNullOrWhiteSpace(dirName) || string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                Directory.CreateDirectory(dirName);
                await using var fstream = new FileStream(fi.FullName, FileMode.Create);
                await entry.BaseEntry.GetReadOnlyStream().CopyToAsync(fstream);
            }

            Logger.Info($"Wrote pakfile contents to {rootPath}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to extract pakfile zip");
        }
    }

    public async Task ExportFiles()
    {
        if (!GetSelection(out IReadOnlyList<Node> nodes, single: false))
            return;

        IStorageFolder? folder = await PickFolder("Pick Export Directory");
        if (folder is null || _pakfileLump is null)
            return;

        var rootPath = folder.Path.AbsolutePath;
        var exportDir = new DirectoryInfo(rootPath);
        if (!exportDir.Exists)
            throw new DirectoryNotFoundException(rootPath);

        var commonParentPath = Node.FindCommonAncestor(nodes).PathString + '/';
        try
        {
            foreach (PakfileEntryViewModel entry in nodes
                         .SelectMany(x => x.IsDirectory ? x.EnumerateLeaves() : [x])
                         .Select(x => x.Leaf)
                         .OfType<PakfileEntryViewModel>())
            {
                FileInfo fi = new(Path.GetFullPath(exportDir.FullName + '/' + entry.Key[commonParentPath.Length..]));
                if (fi.Exists)
                {
                    Logger.Warn($"{fi.FullName} already exists, not overwriting.");
                    continue;
                }

                var dirName = fi.Directory?.FullName;
                if (string.IsNullOrWhiteSpace(dirName) || string.IsNullOrWhiteSpace(entry.Key))
                    continue;

                Directory.CreateDirectory(dirName);
                await using var fstream = new FileStream(fi.FullName, FileMode.Create);
                await entry.BaseEntry.GetReadOnlyStream().CopyToAsync(fstream);

                Logger.Info($"Exported {entry.Key} to {fi.FullName}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to extract pakfile zip");
        }
    }

    // This has a big fuckoff empty label at the top of the dialog, dunno how to get rid of it.
    private static IMsBox<string> CreateMessageBox(string title, string inputLabel, string buttonLabel,
        string defaultInput = "") =>
        MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams {
            ContentTitle = title,
            ShowInCenter = true,
            InputParams = new InputParams { Label = inputLabel, DefaultValue = defaultInput },
            Width = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ButtonDefinitions = new[] {
                new ButtonDefinition { Name = buttonLabel, IsDefault = true },
                new ButtonDefinition { Name = "Cancel", IsCancel = true }
            }
        });

    private static Task<string> ShowMessageBox(IMsBox<string> msBox)
        => msBox.ShowWindowDialogAsync(Program.Desktop.MainWindow);

    private static async ValueTask<IStorageFolder?> PickFolder(string title = "Pick folder")
    {
        if (Program.Desktop.MainWindow is null)
            return null;

        IReadOnlyList<IStorageFolder> result = await Program.Desktop.MainWindow.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title });

        if (result is not { Count: 1 })
            return null;

        return result[0];
    }

    private static async ValueTask<IReadOnlyList<IStorageFile>?> PickFiles(string title = "Pick file(s)")
    {
        if (Program.Desktop.MainWindow is null)
            return null;

        return await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = title, AllowMultiple = true });
    }
}
