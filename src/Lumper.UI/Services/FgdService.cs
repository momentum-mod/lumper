namespace Lumper.UI.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Lumper.Lib.Fgd;
using Lumper.UI.ViewModels.Shared.Entity;
using NLog;
using ReactiveUI;

/// <summary>
/// Dedicated service for loading and serving FGD files. As a service as indepenent from BSPs.
/// </summary>
public sealed class FgdService : ReactiveObject
{
    public static FgdService Instance { get; } = new();

    private string? _path = StateService.Instance.FgdPath;
    public string? Path
    {
        get => _path;
        set
        {
            if (_path == value)
                return;

            _path = value;
            StateService.Instance.FgdPath = value;
            LoadFgdFile();
            this.RaisePropertyChanged();
        }
    }

    public IReadOnlyDictionary<string, FgdEntity> Entities { get; private set; } =
        new Dictionary<string, FgdEntity>(StringComparer.OrdinalIgnoreCase);

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private FgdService()
    {
        LoadFgdFile();
    }

    private void LoadFgdFile()
    {
        if (string.IsNullOrEmpty(Path) || !File.Exists(Path))
        {
            _logger.Warn(
                $"FGD file {(!string.IsNullOrEmpty(Path) ? Path : "<missing>")} does not exist, skipping load"
            );
            return;
        }

        try
        {
            Entities = Fgd.Parse(File.ReadAllText(Path));

            if (BspService.Instance.EntityLumpViewModel is { } entityLumpVm)
            {
                foreach (EntityViewModel entity in entityLumpVm.Entities.Items)
                {
                    entity.ResetFgdProperties();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to parse FGD file {Path ?? ""}");
            Entities = new Dictionary<string, FgdEntity>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public async Task ShowFilePickerDialog()
    {
        if (Program.Desktop.MainWindow is null)
            return;

        IReadOnlyList<IStorageFile> result = await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(
            GenerateFilePickerOptions()
        );

        Path = result.Count > 0 ? result[0].Path.LocalPath : "";
    }

    private static FilePickerOpenOptions GenerateFilePickerOptions()
    {
        return new()
        {
            Title = "Pick FGD File",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("FGD File") { Patterns = ["*.fgd"] }],
        };
    }
}
