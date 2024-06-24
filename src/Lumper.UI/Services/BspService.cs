namespace Lumper.UI.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lib.Bsp.Enum;
using Lib.BSP;
using Lib.BSP.IO;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ViewModels.Shared;
using ViewModels.Shared.Entity;
using ViewModels.Shared.Pakfile;
using Views;

/// <summary>
/// Singleton service handling the currently loaded BSP file.
/// This service has sole responsibility for storing BSP, and all IO work.
/// </summary>
public sealed class BspService : ReactiveObject
{
    /// <summary>
    /// The singleton instance to the service
    /// </summary>
    public static BspService Instance { get; } = new();

    /// <summary>
    /// The currently loaded BSP file
    /// </summary>
    [Reactive]
    public BspFile? BspFile { get; private set; }

    /// <summary>
    /// Returns whether the active BSP file has been modified.
    /// </summary>
    [Reactive]
    public bool IsModified { get; private set; }

    /// <summary>
    /// Returns whether the service is currently doing IO work (loading/saving)
    /// </summary>
    [Reactive]
    public bool IsLoading { get; set; }

    /// <summary>
    /// The name of the BSP file, without .bsp extension. null is no BSP is loaded.
    /// </summary>
    [Reactive]
    public string? FileName { get; private set; }

    /// <summary>
    /// The name of the BSP file, without .bsp extension. null is no BSP is loaded.
    /// </summary>
    [Reactive]
    public string? FilePath { get; private set; }

    /// <summary>
    /// Whether the service current has a loaded BSP file
    /// </summary>
    [Reactive]
    public bool HasLoadedBsp { get; set; }

    [Reactive]
    public bool ShouldCompress { get; set; }

    [Reactive]
    public bool MakeBackup { get; set; } = true;

    private EntityLumpViewModel? _entityLumpViewModel;
    public EntityLumpViewModel? EntityLumpViewModel
        => LazyLoadLump(ref _entityLumpViewModel, () => new EntityLumpViewModel(BspFile!));

    private PakfileLumpViewModel? _pakfileLumpViewModel;
    public PakfileLumpViewModel? PakfileLumpViewModel
        => LazyLoadLump(ref _pakfileLumpViewModel, () => new PakfileLumpViewModel(BspFile!));

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private List<ILumpViewModel?> Lumps => [_entityLumpViewModel, _pakfileLumpViewModel];

    private BspService() =>
        this.WhenAnyValue(x => x.BspFile)
            .Subscribe(_ => UpdateBspProperties());

    private void UpdateBspProperties()
    {
        FileName = BspFile?.Name;
        FilePath = BspFile?.FilePath;
        HasLoadedBsp = BspFile is not null;
    }

    /// <summary>
    /// Load a BSP file from a system file
    /// </summary>
    public async Task<bool> Load(IStorageFile file)
    {
        if (!file.Path.IsFile || file.Path.AbsolutePath is null)
            throw new IOException("Failed to get file path");

        Logger.Info($"Loading {file.Path.AbsolutePath}");
        IsLoading = true;

        return await Load(file.Path.AbsolutePath);
    }

    /// <summary>
    /// Load a BSP file from an absolute system path
    /// </summary>
    public async Task<bool> Load(string path)
    {
        if (HasLoadedBsp)
            Close();

        IsLoading = true;

        IoProgressWindow? progressWindow = null;
        try
        {
            var cts = new CancellationTokenSource();
            var handler = new IoHandler(cts);

            if (Program.Desktop.MainWindow is not null)
            {
                progressWindow = new IoProgressWindow {
                    Title = $"Loading {Path.GetFileName(path)}",
                    Handler = handler
                };
                _ = progressWindow.ShowDialog(Program.Desktop.MainWindow);
            }

            BspFile = await Observable.Start(() => BspFile.FromPath(handler, path), RxApp.TaskpoolScheduler);

            progressWindow?.Close();

            if (handler.Cancelled)
            {
                Close();
                return false;
            }

            ResetLumpViewModels();
        }
        catch (Exception ex)
        {
            Close();
            Logger.Error(ex, "Failed to load BSP file!");
            return false;
        }

        progressWindow?.Close();
        IsLoading = false;
        IsModified = false;
        return true;
    }

    /// <summary>
    /// Save the currently loaded BSP to it's original file path
    /// </summary>
    public async Task<bool> Save() => await Save(null);

    /// <summary>
    /// Save the currently loaded BSP to the given system file
    /// </summary>
    public async Task<bool> Save(IStorageFile? outFile)
    {
        if (BspFile is null)
            return false;

        IsLoading = true;

        foreach (ILumpViewModel? vm in Lumps)
            vm?.UpdateModel(); // Null propagation means we do nothing for unloaded lumps VMs

        try
        {
            IoProgressWindow? progressWindow = null;
            var cts = new CancellationTokenSource();
            var handler = new IoHandler(cts);

            DesiredCompression compress = ShouldCompress
                ? DesiredCompression.Compressed
                : DesiredCompression.Uncompressed;
            var compressString = compress == DesiredCompression.Compressed ? "compressed" : "uncompressed";

            if (Program.Desktop.MainWindow is not null)
            {
                var outName = outFile is not null ? Path.GetFileName(outFile.Path.AbsolutePath) : FileName;
                progressWindow = new IoProgressWindow {
                    Title = $"Saving {outName} ({compressString})",
                    Handler = handler
                };
                _ = progressWindow.ShowDialog(Program.Desktop.MainWindow);
            }

            await Observable.Start(
                () => BspFile.SaveToFile(
                    handler,
                    outFile?.Path.LocalPath,
                    compress: compress,
                    makeBackup: MakeBackup),
                RxApp.TaskpoolScheduler);

            progressWindow?.Close();

            if (handler.Cancelled)
                return false;
        }
        catch (FileLoadException)
        {
            Logger.Warn("Failed to load new file, doing a full UI file load");
            await Observable.Start(() => Load(outFile?.Path.LocalPath ?? FilePath!),
                RxApp.TaskpoolScheduler);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save file!");
            IsLoading = false;
            return false;
        }

        UpdateBspProperties();
        IsLoading = false;
        IsModified = false;
        return true;
    }

    /// <summary>
    /// Close the currently loaded BSP file
    /// </summary>
    public void Close()
    {
        BspFile = null; // This calls UpdateBspProperties
        ResetLumpViewModels();

        IsModified = false;
        IsLoading = false;
    }

    /// <summary>
    /// Writes a JSON summary of the file out to {filename}.json
    /// </summary>
    public void JsonDump()
    {
        var cts = new CancellationTokenSource();
        var handler = new IoHandler(cts);
        Observable.Start(
            () => BspFile?.JsonDump(handler, sortLumps: false, sortProperties: false, ignoreOffset: false),
            RxApp.TaskpoolScheduler);
    }

    /// <summary>
    /// Mark the currently loaded BSP file as modified
    /// </summary>
    public void MarkAsModified()
    {
        if (!HasLoadedBsp || IsModified)
            return;

        IsModified = true;
    }

    public void ResetLumpViewModels()
    {
        ResetLumpViewModel(nameof(EntityLumpViewModel));
        ResetLumpViewModel(nameof(PakfileLumpViewModel));
    }

    /// <summary>
    /// Sets lump models to null, which will raise change notifs for any page that's using them.
    /// If the new value is really null, there's no BSP loaded (see LazyLoadLump).
    /// Even if there is one loaded, the getter will make a new instance of the viewmodel in question.
    /// So whilst we're setting to null here, to consumers it appears that the models just got set
    /// to a real, non-null value.
    /// </summary>
    public void ResetLumpViewModel(string type)
    {
        switch (type)
        {
            case nameof(EntityLumpViewModel):
                _entityLumpViewModel?.Dispose();
                _entityLumpViewModel = null;
                this.RaisePropertyChanged(nameof(EntityLumpViewModel));
                break;
            case nameof(PakfileLumpViewModel):
                _pakfileLumpViewModel?.Dispose();
                _pakfileLumpViewModel = null;
                this.RaisePropertyChanged(nameof(PakfileLumpViewModel));
                break;
        }
    }

    /// <summary>
    /// Used by lump viewmodels that should only be constructed when a BSP is loaded
    /// Saves a lot of null checks!
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public void ThrowIfNoLoadedBsp(
        [CallerFilePath] string? callerFile = null,
        [CallerMemberName] string? callerMember = null)
    {
        if (HasLoadedBsp)
            return;

        Close();
        var source = string.Join(' ', Path.GetFileNameWithoutExtension(callerFile), callerMember);
        var message = $"{source} is requesting a loaded BSP when it shouldn't!";
        Logger.Error(message); // Log this since this error message tends to get lost
        throw new InvalidOperationException(message);
    }

    private T? LazyLoadLump<T>(ref T? backingField, Func<T> newFn) where T : class?, new()
    {
        if (BspFile is null)
            return null;

        if (backingField is not null)
            return backingField;

        return backingField ??= newFn();
    }
}
