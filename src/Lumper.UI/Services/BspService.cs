namespace Lumper.UI.Services;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.UI.ViewModels.Shared;
using Lumper.UI.ViewModels.Shared.Entity;
using Lumper.UI.ViewModels.Shared.Pakfile;
using Lumper.UI.Views;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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

    public EntityLumpViewModel? EntityLumpViewModel =>
        LazyLoadLump(ref _entityLumpViewModel, () => new EntityLumpViewModel(BspFile!));

    private PakfileLumpViewModel? _pakfileLumpViewModel;

    public PakfileLumpViewModel? PakfileLumpViewModel =>
        LazyLoadLump(ref _pakfileLumpViewModel, () => new PakfileLumpViewModel(BspFile!));

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private List<ILumpViewModel?> Lumps => [_entityLumpViewModel, _pakfileLumpViewModel];

    private BspService() => this.WhenAnyValue(x => x.BspFile).Subscribe(_ => UpdateBspProperties());

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
    public async Task<bool> Load(string pathOrUrl)
    {
        pathOrUrl = new Regex("^lumper://").Replace(pathOrUrl, "");

        if (HasLoadedBsp)
            CloseCurrentBsp();

        IoProgressWindow? progressWindow = null;
        IsLoading = true;
        await using Stream? outStream = pathOrUrl.StartsWith("http") ? await HttpDownload(pathOrUrl) : null;

        try
        {
            var cts = new CancellationTokenSource();
            var handler = new IoHandler(cts);

            if (Program.Desktop.MainWindow is not null)
            {
                progressWindow = new IoProgressWindow
                {
                    Title = $"Loading {Path.GetFileName(pathOrUrl)}",
                    Handler = handler,
                };
                _ = progressWindow.ShowDialog(Program.Desktop.MainWindow);
            }

            BspFile = outStream is not null
                ? await Observable.Start(() => BspFile.FromStream(outStream, handler), RxApp.TaskpoolScheduler)
                : await Observable.Start(() => BspFile.FromPath(pathOrUrl, handler), RxApp.TaskpoolScheduler);

            progressWindow?.Close();

            if (handler.Cancelled)
            {
                CloseCurrentBsp();
                return false;
            }

            ResetLumpViewModels();
        }
        catch (Exception ex)
        {
            CloseCurrentBsp();
            Logger.Error(ex, "Failed to load BSP file!");
            return false;
        }
        finally
        {
            progressWindow?.Close();
            IsLoading = false;
        }

        IsModified = false;
        return true;
    }

    private static async Task<Stream?> HttpDownload(string url)
    {
        IoProgressWindow? progressWindow = null;
        var buffer = ArrayPool<byte>.Shared.Rent(80 * 1024);
        var cts = new CancellationTokenSource();
        var handler = new IoHandler(cts);
        var stream = new MemoryStream();
        try
        {
            if (Program.Desktop.MainWindow is not null)
            {
                progressWindow = new IoProgressWindow { Title = $"Downloading {url}", Handler = handler };
                _ = progressWindow.ShowDialog(Program.Desktop.MainWindow);
            }

            using var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token
            );

            await using Stream downloadStream = await response.Content.ReadAsStreamAsync(cts.Token);

            if (response.Content.Headers.ContentLength is null)
            {
                handler.UpdateProgress(0, "Downloading (unknown length)");
                await downloadStream.CopyToAsync(stream, cts.Token);
            }
            else
            {
                int read;
                var length = (int)response.Content.Headers.ContentLength.Value;
                var remaining = length;
                while (
                    !handler.Cancelled
                    && (
                        read = await downloadStream.ReadAsync(
                            buffer.AsMemory(0, int.Min(buffer.Length, remaining)),
                            cts.Token
                        )
                    ) > 0
                )
                {
                    var prog = (float)read / length * 100;
                    handler.UpdateProgress(prog, $"{float.Floor((1 - ((float)remaining / length)) * 100)}%");
                    await stream.WriteAsync(buffer.AsMemory(0, read), cts.Token);
                    remaining -= read;
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                Logger.Info("Download cancelled by user");
            else
                Logger.Error(ex, "Failed to download map!");

            await stream.DisposeAsync();
            return null;
        }
        finally
        {
            progressWindow?.Close();
        }

        if (handler.Cancelled)
            return null;

        return stream;
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

        if (outFile is null && FilePath is null)
            return false;

        IsLoading = true;
        foreach (ILumpViewModel? vm in Lumps)
            vm?.UpdateModel(); // Null propagation means we do nothing for unloaded lumps VMs

        IoProgressWindow? progressWindow = null;
        try
        {
            var cts = new CancellationTokenSource();
            var handler = new IoHandler(cts);

            DesiredCompression compress = ShouldCompress
                ? DesiredCompression.Compressed
                : DesiredCompression.Uncompressed;
            var compressString = compress == DesiredCompression.Compressed ? "compressed" : "uncompressed";

            var outName = outFile is not null ? Path.GetFileName(outFile.Path.AbsolutePath) : FileName;

            if (Program.Desktop.MainWindow is not null)
            {
                progressWindow = new IoProgressWindow
                {
                    Title = $"Saving {outName} ({compressString})",
                    Handler = handler,
                };
                _ = progressWindow.ShowDialog(Program.Desktop.MainWindow);
            }
            // get the cubemaps that will be changed
            Dictionary<string, string> modified = BspFile.GetLump<PakfileLump>().GetCubemapsToChange(outName);
            await Observable.Start(
                () => BspFile.SaveToFile(outFile?.Path.LocalPath, compress: compress, handler, makeBackup: MakeBackup),
                RxApp.TaskpoolScheduler
            );

            if (outName != FileName)
            {
                PakfileLumpViewModel?.UpdateEntries(false);

                // clean up old outdated keys
                foreach (KeyValuePair<string, string> entry in modified)
                {
                    foreach (PakfileEntryViewModel pk in PakfileLumpViewModel?.Entries?.Items)
                    {
                        if (pk.Key == entry.Key)
                            PakfileLumpViewModel.DeleteEntry(pk);
                    }
                }
            }

            if (handler.Cancelled)
                return false;
        }
        catch (FileLoadException)
        {
            Logger.Warn("Failed to load new file, doing a full UI file load");
            await Observable.Start(() => Load(outFile?.Path.LocalPath ?? FilePath!), RxApp.TaskpoolScheduler);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save file!");
            return false;
        }
        finally
        {
            progressWindow?.Close();
            IsLoading = false;
        }

        UpdateBspProperties();
        IsModified = false;
        return true;
    }

    /// <summary>
    /// Close the currently loaded BSP file
    /// </summary>
    public void CloseCurrentBsp()
    {
        BspFile?.Dispose();
        BspFile = null; // This calls UpdateBspProperties
        ResetLumpViewModels();

        IsModified = false;
        IsLoading = false;
    }

    /// <summary>
    /// Writes a JSON summary of the file out to {filename}.json
    /// </summary>
    public void JsonDump(IStorageFile file) =>
        Observable.Start(
            () =>
                BspFile?.JsonDump(
                    file.Path.LocalPath,
                    null,
                    sortLumps: false,
                    sortProperties: false,
                    ignoreOffset: false
                ),
            RxApp.TaskpoolScheduler
        );

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
        ResetLumpViewModel(typeof(EntityLumpViewModel));
        ResetLumpViewModel(typeof(PakfileLumpViewModel));
    }

    /// <summary>
    /// Sets lump models to null, which will raise change notifs for any page that's using them.
    /// If the new value is really null, there's no BSP loaded (see LazyLoadLump).
    /// Even if there is one loaded, the getter will make a new instance of the viewmodel in question.
    /// So whilst we're setting to null here, to consumers it appears that the models just got set
    /// to a real, non-null value.
    /// </summary>
    public void ResetLumpViewModel(Type type)
    {
        if (type == typeof(EntityLumpViewModel))
        {
            _entityLumpViewModel?.Dispose();
            _entityLumpViewModel = null;
            this.RaisePropertyChanged(nameof(EntityLumpViewModel));
        }
        else if (type == typeof(PakfileLumpViewModel))
        {
            _pakfileLumpViewModel?.Dispose();
            _pakfileLumpViewModel = null;
            this.RaisePropertyChanged(nameof(PakfileLumpViewModel));
        }
    }

    /// <summary>
    /// Used by lump viewmodels that should only be constructed when a BSP is loaded
    /// Saves a lot of null checks!
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public void ThrowIfNoLoadedBsp(
        [CallerFilePath] string? callerFile = null,
        [CallerMemberName] string? callerMember = null
    )
    {
        if (HasLoadedBsp)
            return;

        CloseCurrentBsp();
        var source = string.Join(' ', Path.GetFileNameWithoutExtension(callerFile), callerMember);
        var message = $"{source} is requesting a loaded BSP when it shouldn't!";
        Logger.Error(message); // Log this since this error message tends to get lost
        throw new InvalidOperationException(message);
    }

    private T? LazyLoadLump<T>(ref T? backingField, Func<T> newFn)
        where T : class?, new()
    {
        if (BspFile is null)
            return null;

        if (backingField is not null)
            return backingField;

        return backingField ??= newFn();
    }
}
