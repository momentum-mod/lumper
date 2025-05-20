namespace Lumper.UI.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using Lumper.Lib.Bsp.Lumps;
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
public sealed class BspService : ReactiveObject, IDisposable
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
    /// Size of the BSP file on disk, in bytes.
    /// </summary>
    [Reactive]
    public long? FileSize { get; private set; }

    /// <summary>
    /// Whether the service current has a loaded BSP file
    /// </summary>
    [Reactive]
    public bool HasLoadedBsp { get; private set; }

    /// <summary>
    /// Number of compressed non-empty lumps (including game lumps)
    /// </summary>
    [Reactive]
    public int CompressedLumps { get; private set; }

    /// <summary>
    /// Number of non empty lumps (including game lumps)
    /// </summary>
    [Reactive]
    public int NonEmptyLumps { get; private set; }

    [Reactive]
    public string? FileHash { get; private set; }

    private EntityLumpViewModel? _entityLumpViewModel;

    /// <summary>
    /// Get the entity lump viewmodel. If this is the first time it's being accessed, it will be initialized.
    /// </summary>
    public EntityLumpViewModel? EntityLumpViewModel =>
        LazyLoadLump(ref _entityLumpViewModel, () => new EntityLumpViewModel(BspFile!));

    /// <summary>
    /// Get the entity lump viewmodel if it's been initialized, otherwise returns null.
    /// </summary>
    public EntityLumpViewModel? EntityLumpViewModelLazy => _entityLumpViewModel;

    private PakfileLumpViewModel? _pakfileLumpViewModel;

    /// <summary>
    /// Get the pakfile lump viewmodel. If this is the first time it's being accessed, it will be initialized.
    /// </summary>
    public PakfileLumpViewModel? PakfileLumpViewModel =>
        LazyLoadLump(ref _pakfileLumpViewModel, () => new PakfileLumpViewModel(BspFile!));

    /// <summary>
    /// Get the pakfile lump viewmodel if it's been initialized, otherwise return null.
    /// </summary>
    public PakfileLumpViewModel? PakfileLumpViewModelLazy => _pakfileLumpViewModel;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private List<BspNode?> Lumps => [_entityLumpViewModel, _pakfileLumpViewModel];

    private BspService()
    {
        this.WhenAnyValue(x => x.BspFile).Subscribe(_ => OnBspChanged());

        _bspSubject.Subscribe(bsp =>
        {
            if (bsp?.FilePath is not null)
                StateService.Instance.UpdateRecentFiles(bsp.FilePath);

            FileName = bsp?.Name;
            FilePath = bsp?.FilePath;
            FileSize = bsp?.FileSize;
            HasLoadedBsp = bsp is not null;

            if (bsp is not null)
            {
                IEnumerable<Lump> lumps =
                [
                    .. bsp.Lumps.Values.Where(y => y.Type != BspLumpType.GameLump),
                    .. bsp.GetLump<GameLump>().Lumps.Values.OfType<Lump>(),
                ];
                var nonEmpty = lumps.Where(y => !y.Empty).ToList();
                CompressedLumps = nonEmpty.Sum(y => y.IsCompressed ? 1 : 0);
                NonEmptyLumps = nonEmpty.Count;
            }
            else
            {
                CompressedLumps = 0;
                NonEmptyLumps = 0;
            }
        });

        _bspSubject
            .Select(bsp => Observable.FromAsync(async () => FileHash = bsp is not null ? await bsp.FileHash : ""))
            .Switch()
            .Subscribe();
    }

    // Initial subject to mark BSP changes, can't get RaisePropertyChanged(nameof(BspFile)) to work
    private readonly Subject<BspFile?> _bspSubject = new();

    private void OnBspChanged()
    {
        _bspSubject.OnNext(BspFile);
    }

    /// <summary>
    /// Load a BSP file from a system file
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
    /// Load a BSP file from an absolute system path
    /// </summary>
    public async Task<bool> Load(string pathOrUrl)
    {
        if (pathOrUrl.StartsWith("lumper://", StringComparison.Ordinal))
        {
            pathOrUrl = pathOrUrl[9..];
            // Sanitised URL produced by the dashboard is in form lumper://https//cdn.momentum..., (missing colon),
            // add it back.
            pathOrUrl = new Regex("(?<=http|https)//").Replace(pathOrUrl, "://");
        }

        if (HasLoadedBsp)
            CloseCurrentBsp();

        IoProgressWindow? progressWindow = null;
        IsLoading = true;
        await using Stream? outStream = pathOrUrl.StartsWith("http", StringComparison.Ordinal)
            ? await HttpDownload(pathOrUrl)
            : null;

        try
        {
            var cts = new CancellationTokenSource();
            var handler = new IoHandler(cts);

            progressWindow = new IoProgressWindow
            {
                Title = $"Loading {Path.GetFileName(pathOrUrl)}",
                Handler = handler,
            };
            _ = progressWindow.ShowDialog(Program.MainWindow);

            BspFile = outStream is not null
                ? await Observable.Start(() => BspFile.FromStream(outStream, handler), RxApp.TaskpoolScheduler)
                : await Observable.Start(() => BspFile.FromPath(pathOrUrl, handler), RxApp.TaskpoolScheduler);

            progressWindow.Close();

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
            _logger.Error(ex, "Failed to load BSP file!");
            return false;
        }
        finally
        {
            progressWindow?.Close();
            IsLoading = false;
        }

        OnBspChanged();
        IsModified = false;
        return true;
    }

    private async Task<Stream?> HttpDownload(string url)
    {
        IoProgressWindow? progressWindow = null;
        byte[] buffer = new byte[80 * 1024];
        var cts = new CancellationTokenSource();
        var handler = new IoHandler(cts);
        var stream = new MemoryStream();
        try
        {
            progressWindow = new IoProgressWindow { Title = $"Downloading {url}", Handler = handler };
            _ = progressWindow.ShowDialog(Program.MainWindow);

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
                int length = (int)response.Content.Headers.ContentLength.Value;
                int remaining = length;
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
                    float prog = (float)read / length * 100;
                    handler.UpdateProgress(prog, $"{float.Floor((1 - ((float)remaining / length)) * 100)}%");
                    await stream.WriteAsync(buffer.AsMemory(0, read), cts.Token);
                    remaining -= read;
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                _logger.Info("Download cancelled by user");
            else
                _logger.Error(ex, "Failed to download map!");

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
    public async Task<bool> Save()
    {
        return await Save(null);
    }

    /// <summary>
    /// Save the currently loaded BSP to the given system file
    /// </summary>
    public async Task<bool> Save(IStorageFile? outFile)
    {
        if (BspFile is null)
            return false;

        if (outFile is null && (FilePath is null || FileName is null))
            return false;

        IsLoading = true;

        PushChangesToModel();

        IoProgressWindow? progressWindow = null;
        try
        {
            var cts = new CancellationTokenSource();
            var handler = new IoHandler(cts);

            DesiredCompression compress = StateService.Instance.SaveCompressed
                ? DesiredCompression.Compressed
                : DesiredCompression.Uncompressed;
            string compressString = compress == DesiredCompression.Compressed ? "compressed" : "uncompressed";

            string outName = outFile is not null ? Path.GetFileName(outFile.Path.AbsolutePath) : FileName!;

            progressWindow = new IoProgressWindow { Title = $"Saving {outName} ({compressString})", Handler = handler };
            _ = progressWindow.ShowDialog(Program.MainWindow);

            await Observable.Start(
                () =>
                    BspFile.SaveToFile(
                        outFile?.Path.LocalPath,
                        new BspFile.SaveToFileOptions
                        {
                            Compression = compress,
                            Handler = handler,
                            MakeBackup = StateService.Instance.MakeBackup,
                            RenameMapFiles = StateService.Instance.RenameMapFiles,
                        }
                    ),
                RxApp.TaskpoolScheduler
            );

            // If we've renamed cubemaps, need to refresh the pakfile VM.
            if (outName != FileName && PakfileLumpViewModel is not null && StateService.Instance.RenameMapFiles)
                PakfileLumpViewModel?.PullChangesFromModel();

            if (handler.Cancelled)
                return false;
        }
        catch (FileLoadException)
        {
            _logger.Warn("Failed to load new file, doing a full UI file load");
            await Observable.Start(() => Load(outFile?.Path.LocalPath ?? FilePath!), RxApp.TaskpoolScheduler);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save file!");
            return false;
        }
        finally
        {
            progressWindow?.Close();
            IsLoading = false;
        }

        OnBspChanged();
        IsModified = false;
        return true;
    }

    /// <summary>
    /// Close the currently loaded BSP file
    /// </summary>
    public void CloseCurrentBsp()
    {
        if (!HasLoadedBsp)
            return;

        if (FilePath is not null)
            StateService.Instance.UpdateRecentFiles(FilePath);

        BspFile?.Dispose();
        BspFile = null;
        ResetLumpViewModels();

        IsModified = false;
        IsLoading = false;
    }

    /// <summary>
    /// Writes a JSON summary of the file out to {filename}.json
    /// </summary>
    public void JsonDump(IStorageFile file)
    {
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

    public void PushChangesToModel()
    {
        foreach (BspNode? lump in Lumps)
            lump?.PushChangesToModel();
    }

    private void ResetLumpViewModels()
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
        string source = string.Join(' ', Path.GetFileNameWithoutExtension(callerFile), callerMember);
        string message = $"{source} is requesting a loaded BSP when it shouldn't!";
        _logger.Error(message); // Log this since this error message tends to get lost
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

    public void Dispose()
    {
        _entityLumpViewModel?.Dispose();
        _pakfileLumpViewModel?.Dispose();
        _bspSubject.Dispose();
        BspFile?.Dispose();
    }
}
