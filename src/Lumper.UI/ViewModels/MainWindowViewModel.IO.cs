using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.IO;
using Lumper.UI.ViewModels.Bsp;
using Lumper.UI.ViewModels.Bsp.Lumps;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using ReactiveUI;

namespace Lumper.UI.ViewModels;

// MainWindowViewModel support for reading and writing of <see cref="BspFile"/>.
public partial class MainWindowViewModel
{
    private void IOInit()
    {
        this.WhenAnyValue(x => x.BspModel)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(m => m is not null)
            .Subscribe(x =>
                BspModel!.RaisePropertyChanged(nameof(BspModel.FilePath)));
    }

    private static IReadOnlyList<FilePickerFileType> GenerateBspFileFilter()
    {
        var bspFilter = new FilePickerFileType("BSP files");
        bspFilter.Patterns = new[] { "*.bsp" };
        //MIME references from:
        //https://www.wikidata.org/wiki/Q105858735
        //https://www.wikidata.org/wiki/Q105859836
        //https://www.wikidata.org/wiki/Q2701652
        bspFilter.MimeTypes = new[]
        {
            "application/octet-stream", "model/vnd.valve.source.compiled-map"
        };

        var anyFilter = new FilePickerFileType("All files");
        anyFilter.Patterns = new[] { "*" };

        return new[] { bspFilter, anyFilter };
    }

    public async ValueTask OpenCommand()
    {
        if (Desktop.MainWindow is null)
            return;

        var dialog = new FilePickerOpenOptions();
        dialog.AllowMultiple = false;
        dialog.Title = "Pick BSP file";
        dialog.FileTypeFilter = GenerateBspFileFilter();
        var result = await Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(dialog);
        if (result is not { Count: 1 })
            return;
        await LoadBsp(result[0]);
    }

    public async ValueTask SaveCommand()
    {
        if (_bspModel is null)
            return;

        if (_bspModel.FilePath is null)
        {
            if (Desktop.MainWindow is null)
                return;
            var dialog = new FilePickerSaveOptions();
            dialog.Title = "Pick BSP file";
            dialog.FileTypeChoices = GenerateBspFileFilter();
            var result = await Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
            if (result is null)
                return;
            Save(result);
        }
        else
        {
            Save(_bspModel.FilePath);
        }
    }

    public async ValueTask SaveAsCommand()
    {
        if (Desktop.MainWindow is null || _bspModel is null)
            return;
        var dialog = new FilePickerSaveOptions();
        dialog.Title = "Pick BSP file";
        dialog.FileTypeChoices = GenerateBspFileFilter();
        var result = await Desktop.MainWindow.StorageProvider.SaveFilePickerAsync(dialog);
        if (result is null)
            return;
        Save(result);
    }

    private async void Save(IStorageFile file)
    {
        if (_bspModel is null || !file.CanOpenWrite)
            return;

        try
        {
            Content = null;
            IsProgressBarVisible = true;

            //TODO: Copy bsp model tree for fallback if error occurs
            await Task.Run(_bspModel.Update);
            await using var writer =
                new BspFileWriter(_bspModel.BspFile, await file.OpenWriteAsync());
            await Task.Run(writer.Save);

            IsProgressBarVisible = false;
            Content = _bspModel;
        }
        catch (Exception e)
        {
            MessageBoxManager.GetMessageBoxStandardWindow("Error",
                $"Error while saving file \n{e.Message}");
            return;
        }

        _bspModel.FilePath = file.Name;
    }

    private async void Save(string path)
    {
        if (_bspModel is null)
            return;

        try
        {
            using (var stream = File.OpenWrite(path))
            {
                Content = null;
                IsProgressBarVisible = true;

                //TODO: Copy bsp model tree for fallback if error occurs
                await Task.Run(_bspModel.Update);
                await using var writer =
                    new BspFileWriter(_bspModel.BspFile, stream);
                await Task.Run(writer.Save);

                IsProgressBarVisible = false;
                Content = _bspModel;
            }
        }
        catch (Exception e)
        {
            MessageBoxManager.GetMessageBoxStandardWindow("Error",
                $"Error while saving file \n{e.Message}");
            return;
        }

        _bspModel.FilePath = path;
    }

    private async void LoadBsp(string path)
    {
        BspModel = null;
        Content = null;

        var bspFile = new BspFile(path);

        BspModel = new BspViewModel(bspFile);
        TasksModel = new Tasks.TasksViewModel(bspFile);
        IsProgressBarVisible = true;

        if (BspModel.BspNode is BspNodeViewModel bspNodeViewModel)
            await bspNodeViewModel.InitializeAsync();

        Content = BspModel;
        IsProgressBarVisible = false;
    }

    private async Task LoadBsp(IStorageFile file)
    {
        if (!file.CanOpenRead)
            return;
        Console.WriteLine(file.Name);
        var folder = await file.GetParentAsync();
        if (!file.TryGetUri(out var path))
        {
            throw new Exception("Failed to get file path");
        }
        LoadBsp(path.AbsolutePath);
    }

    public void BspToJsonCommand()
    {
        if (BspModel is null)
            return;
        BspModel.BspFile.ToJson(false, false, false);
    }

    public async Task CloseCommand()
    {
        if (_bspModel is null || !_bspModel.BspNode.IsModifiedRecursive)
            return;

        var messageBox = MessageBoxManager
            .GetMessageBoxStandardWindow("You have unsaved changes",
                "Do you want to discard changes?", ButtonEnum.OkCancel);
        var result = await messageBox.ShowDialog(Desktop.MainWindow);
        if (result != ButtonResult.Ok)
            return;
        BspModel = null;
    }

    public void ExitCommand()
    {
        Desktop.MainWindow?.Close();
    }

    public async Task OnClose(CancelEventArgs e)
    {
        e.Cancel = true;
        if (_bspModel is not null && _bspModel.BspNode.IsModifiedRecursive)
        {
            var messageBox = MessageBoxManager.GetMessageBoxStandardWindow(
                "You have unsaved changes", "Do you want to close application without saving?",
                ButtonEnum.OkCancel);
            var result = await messageBox.ShowDialog(Desktop.MainWindow);
            if (result != ButtonResult.Ok)
                return;
        }

        //Since we have to cancel closing event on start due to not being able to await on Event
        //and message box cannot work in synchronous mode due to main window thread being frozen,
        //we have to manually close process. (Window.Close() would recursively call OnClose function)
        Environment.Exit(1);
    }
}
