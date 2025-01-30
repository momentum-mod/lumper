namespace Lumper.UI.ViewModels;

using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Lumper.Lib.Util;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.LogViewer;
using Lumper.UI.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using NLog;
using ReactiveUI;

public class MainWindowViewModel : ViewModel
{
    public static BspService BspService => BspService.Instance;
    public static PageService PageService => PageService.Instance;
    public static UpdaterService UpdaterService => UpdaterService.Instance;
    public static StateService StateService => StateService.Instance;

    public LogViewerViewModel LogViewer { get; set; } = new();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public MainWindowViewModel()
    {
        Program.Desktop.Startup += (_, _) => Program.MainWindow.Loaded += (_, _) => OnMainWindowOpened();

        // Preload the asset manifest in the background
        Observable.Start(AssetManifest.Preload, RxApp.TaskpoolScheduler);
    }

    private static void OnMainWindowOpened()
    {
        if (Program.Desktop.Args is [{ } bspFile])
            Observable.Start(() => BspService.Instance.Load(bspFile), RxApp.MainThreadScheduler);
    }

    public async Task OpenCommand()
    {
        var dialog = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Pick BSP File",
            FileTypeFilter = GenerateBspFileFilter(),
        };

        IReadOnlyList<IStorageFile> result = await Program.MainWindow.StorageProvider.OpenFilePickerAsync(dialog);

        if (result.Count == 0)
            return;
        if (result.Count > 1)
            Logger.Warn("Lumper is only capable of loading a single BSP at a time. Loading first file provided...");

        await BspService.Instance.Load(result[0]);
    }

    public async Task OpenUrlCommand()
    {
        IMsBox<string> msBox = MessageBoxManager.GetMessageBoxCustom(
            new MessageBoxCustomParams
            {
                ContentTitle = "Load from URL",
                ShowInCenter = true,
                Width = 400,
                InputParams = new InputParams(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ButtonDefinitions =
                [
                    new ButtonDefinition { Name = "Load", IsDefault = true },
                    new ButtonDefinition { Name = "Cancel", IsCancel = true },
                ],
            }
        );

        string result = await msBox.ShowWindowDialogAsync(Program.MainWindow);
        string url = msBox.InputValue;

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
        if (!BspService.Instance.HasLoadedBsp)
            return;

        var dialog = new FilePickerSaveOptions
        {
            Title = "Save BSP File",
            DefaultExtension = ".bsp",
            FileTypeChoices = GenerateBspFileFilter(),
        };

        IStorageFile? result = await Program.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
        if (result is null)
            return;

        await BspService.Instance.Save(result);
    }

    public async Task JsonDumpCommand()
    {
        if (!BspService.Instance.HasLoadedBsp)
            return;

        var dialog = new FilePickerSaveOptions
        {
            Title = "Export JSON Summary",
            DefaultExtension = ".json",
            FileTypeChoices = [new FilePickerFileType("JSON File") { Patterns = ["*.json"] }],
        };

        IStorageFile? result = await Program.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
        if (result is null)
            return;

        BspService.Instance.JsonDump(result);
    }

    public async Task CloseCommand()
    {
        if (
            BspService.Instance.HasLoadedBsp
            && BspService.Instance.IsModified
            && await ShowUnsavedChangesDialog("Do you want to discard your current changes?")
        )
            BspService.Instance.CloseCurrentBsp();
    }

    public void ExitCommand() => Program.MainWindow.Close();

    public void AboutCommand()
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog(Program.MainWindow);
    }

    public static async ValueTask OnClose(WindowClosingEventArgs e)
    {
        if (!BspService.Instance.IsModified)
            return;

        // We need to cancel this immediately otherwise window closes before we can await the dialog
        e.Cancel = true;

        if (await ShowUnsavedChangesDialog("Are you sure you want to close the application without saving?"))
            Program.Desktop.Shutdown();
    }

    private static Task<bool> ShowUnsavedChangesDialog(string message) =>
        MessageBoxManager
            .GetMessageBoxStandard("Unsaved changes", message, ButtonEnum.YesNo)
            .ShowWindowDialogAsync(Program.MainWindow)
            .ContinueWith(result => result.Result == ButtonResult.Yes);

    private static FilePickerFileType[] GenerateBspFileFilter() =>
        [
            new("BSP Files")
            {
                Patterns = ["*.bsp"],
                // MIME references from:
                // https://www.wikidata.org/wiki/Q105858735
                // https://www.wikidata.org/wiki/Q105859836
                // https://www.wikidata.org/wiki/Q2701652
                MimeTypes = ["application/octet-stream", "model/vnd.valve.source.compiled-map"],
            },
            new("All Files") { Patterns = ["*"] },
        ];
}
