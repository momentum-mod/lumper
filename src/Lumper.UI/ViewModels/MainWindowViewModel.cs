namespace Lumper.UI.ViewModels;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LogViewer;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
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
            Observable.Start(
                () => BspService.Instance.Load(Program.Desktop.Args[0]),
                RxApp.MainThreadScheduler);
        }
    }

    public async Task OpenCommand()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions {
            AllowMultiple = false, Title = "Pick BSP File", FileTypeFilter = GenerateBspFileFilter()
        };

        IReadOnlyList<IStorageFile> result =
            await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(dialog);

        if (result.Count == 0)
            return;
        if (result.Count > 1)
            Logger.Warn("Lumper is only capable of loading a single BSP at a time. Loading first file provided...");

        await BspService.Instance.Load(result[0]);
    }
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

        var dialog = new FilePickerSaveOptions { Title = "Pick BSP File", FileTypeChoices = GenerateBspFileFilter() };

        IStorageFile? result = await Program.Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
        if (result is null)
            return;

        await BspService.Instance.Save(result);
    }

    public async Task CloseCommand()
    {
        if (!BspService.Instance.HasLoadedBsp)
            return;

        if (BspService.Instance.IsModified)
        {
            ButtonResult result = await MessageBoxManager
                .GetMessageBoxStandard(
                    "Unsaved changes",
                    "Do you want to discard your changes?", ButtonEnum.OkCancel)
                .ShowWindowDialogAsync(Program.Desktop.MainWindow);

            if (result != ButtonResult.Ok)
                return;
        }

        BspService.Instance.Close();
    }

    public void ExitCommand() => Program.Desktop.MainWindow?.Close();

    public static async Task OnClose(WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (BspService.Instance.IsModified)
        {
            ButtonResult result = await MessageBoxManager
                .GetMessageBoxStandard(
                    "Unsaved changes",
                    "Are you sure you want to close the application without saving?",
                    ButtonEnum.OkCancel)
                .ShowWindowDialogAsync(Program.Desktop.MainWindow);

            if (result != ButtonResult.Ok)
                return;
        }

        // Since we have to cancel closing event on start due to not being able to await on Event
        // and message box cannot work in synchronous mode due to main window thread being frozen,
        // we have to manually close process. (Window.Close() would recursively call OnClose function)
        Environment.Exit(1);
    }

    private static FilePickerFileType[] GenerateBspFileFilter() => [
        new FilePickerFileType("BSP Files") {
            Patterns = new[] { "*.bsp" },
            // MIME references from:
            // https://www.wikidata.org/wiki/Q105858735
            // https://www.wikidata.org/wiki/Q105859836
            // https://www.wikidata.org/wiki/Q2701652
            MimeTypes = new[] { "application/octet-stream", "model/vnd.valve.source.compiled-map" }
        },
        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
    ];
}
