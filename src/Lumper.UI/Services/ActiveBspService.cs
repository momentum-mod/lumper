namespace Lumper.UI.Services;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lib.BSP;
using Lib.BSP.IO;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

/// <summary>
///     Singleton service handling the currently loaded BSP file.<br/><br/>
///     This service has sole responsibility for storing BSP, and all IO work.
/// </summary>
public sealed class ActiveBspService : ReactiveObject
{
    /// <summary>
    ///     The singleton instance to the service
    /// </summary>
    public static ActiveBspService Instance { get; } = new();
    private ActiveBspService() { }

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     The currently loaded BSP file
    /// </summary>
    [Reactive]
    public BspFile? BspFile { get; set; }

    /// <summary>
    ///     Returns whether the active BSP file has been modified.
    /// </summary>
    [Reactive]
    public bool IsModified { get; private set; } = false;

    /// <summary>
    ///     Returns whether the service is currently doing IO work (loading/saving)
    /// </summary>
    [Reactive]
    public bool IsLoading { get; set; }

    /// <summary>
    ///     The name of the BSP file, without .bsp extension. null is no BSP is loaded.
    /// </summary>
    public IObservable<string?> FileName => this.WhenAnyValue(x => x.BspFile).Select(x => x?.Name);

    /// <summary>
    ///     The name of the BSP file, without .bsp extension. null is no BSP is loaded.
    /// </summary>
    public IObservable<string?> FilePath => this.WhenAnyValue(x => x.BspFile).Select(x => x?.FilePath);

    /// <summary>
    ///     Observable that notifies whenever a BSP is unloaded
    /// </summary>
    public IObservable<BspFile?> FileChanged => this.WhenAnyValue(x => x.BspFile);

    /// <summary>
    ///     Observable that notifies whenever a BSP is unloaded
    /// </summary>
    public IObservable<BspFile?> BspUnloaded => this.WhenAnyValue(x => x.BspFile).Where(x => x is null);

    /// <summary>
    ///     Observable that notifies whenever a BSP is loaded
    /// </summary>
    public IObservable<BspFile?> BspLoaded => this.WhenAnyValue(x => x.BspFile).Where(x => x is not null); // TODO: When does this actually fire? When a new BSP is laoded, or whenever a BSP changes?

    /// <summary>
    ///     Returns whether the service current has a loaded BSP file
    /// </summary>
    public bool HasLoadedBsp => BspFile is not null;

    /// <summary>
    ///     Load a BSP file from a system file
    /// </summary>
    public async Task<bool> Load(IStorageFile file)
    {
        if (!file.Path.IsFile || file.Path.AbsolutePath is null)
            throw new IOException("Failed to get file path");

        _logger.Info($"Loading {file.Path.AbsolutePath}");
        IsLoading = true;

        return await Load(file.Path.AbsolutePath);
    }

    /// <summary>
    ///     Load a BSP file from an absolute system path
    /// </summary>
    public async Task<bool> Load(string path)
    {
        Close();

        IsLoading = true;

        try
        {
            BspFile = await Observable.Start(() => new BspFile(path), RxApp.TaskpoolScheduler);
        }
        catch (Exception e)
        {
            Close();
            _logger.Error($"Failed to load BSP file! {e.GetType().Name}: ${e.Message}");
            return false;
        }

        IsLoading = false;
        return true;
    }

    /// <summary>
    ///     Save the currently loaded BSP to it's original file path
    /// </summary>
    public async Task<bool> Save() => await Save(null);

    /// <summary>
    ///     Save the currently loaded BSP to the given system file
    /// </summary>
    public async Task<bool> Save(IStorageFile? file)
    {
        if (BspFile is null)
            return false;

        IsLoading = true;
        try
        {
            // TODO: old jpaja stuff idk
            // // TODO: Copy bsp model tree for fallback if error occurs
            // BspModel.Update();
            Stream stream;
            if (file is null)
            {
                if (BspFile.FilePath is null)
                    return false;

                stream = File.OpenWrite(BspFile.FilePath);
            }
            else
            {
                stream = await file.OpenWriteAsync();
            }

            await using var writer = new BspFileWriter(BspFile, stream);
            writer.Save();
        }

        catch (Exception e)
        {
            _logger.Error($"Failed to save file! {e.GetType().Name}: {e.Message}");
            return false;
        }

        IsLoading = false;
        IsModified = false;
        return true;
    }

    /// <summary>
    ///     Close the currently loaded BSP file
    /// </summary>
    public void Close()
    {
        BspFile = null;
        IsModified = false;
        IsLoading = false;
    }

    /// <summary>
    ///     Mark the currently loaded BSP file as modified
    /// </summary>
    public void MarkAsModified()
    {
        if (!HasLoadedBsp || IsModified)
            return;

        IsModified = true;
    }
}
