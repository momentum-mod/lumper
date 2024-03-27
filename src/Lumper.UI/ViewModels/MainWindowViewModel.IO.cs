namespace Lumper.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Lumper.UI.ViewModels.Bsp;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using Services;

// MainWindowViewModel support for reading and writing of <see cref="BspFile"/>.
public partial class MainWindowViewModel
{
    public static ActiveBspService BspService => ActiveBspService.Instance;
    private void IOInit()
    {
        this.WhenAnyValue(x => x.BspModel)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(m => m is not null)
            .Subscribe(x =>
                BspModel!.RaisePropertyChanged(nameof(BspModel.FilePath)));

        RxApp.MainThreadScheduler.Schedule(OnLoad);
    }

    public async Task OpenCommand()
    {
        if (Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Pick BSP File",
            FileTypeFilter = GenerateBspFileFilter()
        };

        IReadOnlyList<IStorageFile> result = await Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(dialog);
        if (result.Count == 0)
            return;
        if (result.Count > 1)
            _logger.Warn("Lumper is only capable of loading a single BSP at once. Loading first file provided...");

        _logger.Info($"Loading {result[0].Path.AbsolutePath}");

        if (!await ActiveBspService.Instance.Load(result[0]))
            MessageBoxManager.GetMessageBoxStandard("Error loading BSP", "Failed to load BSP file! See log panel for error.");

        LoadDefaultPage();
    }

    public async Task SaveCommand()
    {
        if (BspModel.FilePath is null)
        {
            await SaveAsCommand();
        }
        else
        {
            if (!await ActiveBspService.Instance.Save())
                MessageBoxManager.GetMessageBoxStandard("Error saving BSP", "Failed to save BSP file! See log panel for error.");
        }
    }

    public async Task SaveAsCommand()
    {
        if (Desktop.MainWindow is null || ActiveBspService.Instance.BspFile is null)
            return;

        var dialog = new FilePickerSaveOptions
        {
            Title = "Pick BSP file",
            FileTypeChoices = GenerateBspFileFilter()
        };

        IStorageFile? result = await Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
        if (result is null)
            return;

        if (!await ActiveBspService.Instance.Save(result))
            MessageBoxManager.GetMessageBoxStandard("Error saving BSP", "Failed to save BSP file! See log panel for error.");
    }

    public void BspToJsonCommand() => BspModel?.BspFile.ToJson(false, false, false);

    public async Task CloseCommand()
    {
        if (ActiveBspService.Instance.IsModified)
            return;

        ButtonResult result = await MessageBoxManager
            .GetMessageBoxStandard(
                "You have unsaved changes",
                "Do you want to discard changes?", ButtonEnum.OkCancel)
            .ShowWindowDialogAsync(Desktop.MainWindow);

        if (result != ButtonResult.Ok)
            return;

        ActiveBspService.Instance.Close();
    }

    public void ExitCommand() => Desktop.MainWindow?.Close();

    public async Task OnClose(WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (ActiveBspService.Instance.IsModified)
        {
            ButtonResult result = await MessageBoxManager
                .GetMessageBoxStandard(
                    "You have unsaved changes",
                    "Do you want to close application without saving?",
                    ButtonEnum.OkCancel)
                .ShowWindowDialogAsync(Desktop.MainWindow);

            if (result != ButtonResult.Ok)
                return;
        }

        // Since we have to cancel closing event on start due to not being able to await on Event
        // and message box cannot work in synchronous mode due to main window thread being frozen,
        // we have to manually close process. (Window.Close() would recursively call OnClose function)
        Environment.Exit(1);
    }

    private static FilePickerFileType[] GenerateBspFileFilter() =>
    [
        new FilePickerFileType("BSP Files")
        {
            Patterns = new[]
            {
                "*.bsp"
            },
            // MIME references from:
            // https://www.wikidata.org/wiki/Q105858735
            // https://www.wikidata.org/wiki/Q105859836
            // https://www.wikidata.org/wiki/Q2701652
            MimeTypes = new[]
            {
                "application/octet-stream", "model/vnd.valve.source.compiled-map"
            }
        },
        new FilePickerFileType("All Files")
        {
            Patterns = new[]
            {
                "*"
            }
        }
    ];
}
