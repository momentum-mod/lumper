namespace Lumper.UI.ViewModels;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LogViewer;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using NLog;
using ReactiveUI;
using Services;

public class MainWindowViewModel : ViewModel
{
    public static BspService BspService => BspService.Instance;
    public static PageService PageService => PageService.Instance;

    public LogViewerViewModel LogViewer { get; set; } = new();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public MainWindowViewModel()
    {
        if (Program.Desktop.Args is { Length: 1 })
        {
            Observable.Start(() => BspService.Instance.Load(Program.Desktop.Args[0]), RxApp.MainThreadScheduler);
        }
    }

    public async Task OpenCommand()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Pick BSP File",
            FileTypeFilter = GenerateBspFileFilter(),
        };

        IReadOnlyList<IStorageFile> result = await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(
            dialog
        );

        if (result.Count == 0)
            return;
        if (result.Count > 1)
            Logger.Warn("Lumper is only capable of loading a single BSP at a time. Loading first file provided...");

        await BspService.Instance.Load(result[0]);
    }

    public async Task OpenUrlCommand()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        IMsBox<string>? msBox = MessageBoxManager.GetMessageBoxCustom(
            new MessageBoxCustomParams
            {
                ContentTitle = "Load from URL",
                ShowInCenter = true,
                Width = 400,
                InputParams = new InputParams(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ButtonDefinitions = new[]
                {
                    new ButtonDefinition { Name = "Load", IsDefault = true },
                    new ButtonDefinition { Name = "Cancel", IsCancel = true },
                },
            }
        );

        var result = await msBox.ShowWindowDialogAsync(Program.Desktop.MainWindow);
        var url = msBox.InputValue;

        if (result == "Cancel" || !url.StartsWith("http"))
            return;

        await BspService.Load(url);
    }

    public async Task SaveCommand()
    {
        if (BspService.Instance.BspFile?.FilePath is null)
        {
            await SaveAsCommand();
        }

        await BspService.Instance.Save();
    }

    public async Task SaveAsCommand()
    {
        if (Program.Desktop.MainWindow is null || !BspService.Instance.HasLoadedBsp)
            return;

        var dialog = new FilePickerSaveOptions
        {
            Title = "Save BSP File",
            DefaultExtension = ".bsp",
            FileTypeChoices = GenerateBspFileFilter(),
        };

        IStorageFile? result = await Program.Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
        if (result is null)
            return;

        await BspService.Instance.Save(result);
    }

    public async Task JsonDumpCommand()
    {
        if (Program.Desktop.MainWindow is null || !BspService.Instance.HasLoadedBsp)
            return;

        var dialog = new FilePickerSaveOptions
        {
            Title = "Export JSON Summary",
            DefaultExtension = ".json",
            FileTypeChoices = new[] { new FilePickerFileType("JSON File") { Patterns = ["*.json"] } },
        };

        IStorageFile? result = await Program.Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
        if (result is null)
            return;

        BspService.Instance.JsonDump(result);
    }

    public async Task CloseCommand()
    {
        if (
            !BspService.Instance.HasLoadedBsp
            || !await ShowUnsavedChangesDialog("Do you want to discard your current changes?")
        )
            return;

        BspService.Instance.CloseCurrentBsp();
    }

    public void ExitCommand() => Program.Desktop.MainWindow?.Close();

    public static async Task OnClose(WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (!await ShowUnsavedChangesDialog("Are you sure you want to close the application without saving?"))
            return;

        // Since we have to cancel closing event on start due to not being able to await on Event
        // and message box cannot work in synchronous mode due to main window thread being frozen,
        // we have to manually close process. (Window.Close() would recursively call OnClose function)
        Environment.Exit(1);
    }

    private static async Task<bool> ShowUnsavedChangesDialog(string message)
    {
        if (!BspService.Instance.IsModified)
            return true;

        ButtonResult result = await MessageBoxManager
            .GetMessageBoxStandard("Unsaved changes", message, ButtonEnum.OkCancel)
            .ShowWindowDialogAsync(Program.Desktop.MainWindow);

        return result == ButtonResult.Ok;
    }

    private static FilePickerFileType[] GenerateBspFileFilter() =>
        [
            new FilePickerFileType("BSP Files")
            {
                Patterns = new[] { "*.bsp" },
                // MIME references from:
                // https://www.wikidata.org/wiki/Q105858735
                // https://www.wikidata.org/wiki/Q105859836
                // https://www.wikidata.org/wiki/Q2701652
                MimeTypes = new[] { "application/octet-stream", "model/vnd.valve.source.compiled-map" },
            },
            new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
        ];
}
