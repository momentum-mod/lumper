namespace Lumper.UI.ViewModels.StatusBar;

using System;
using System.Reactive.Linq;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.UI.Converters;
using Lumper.UI.Services;
using Lumper.UI.Views.StatusBar;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public sealed class StatusBarViewModel : ViewModelWithView<StatusBarViewModel, StatusBarView>
{
    public static BspService BspService => BspService.Instance;

    [ObservableAsProperty]
    public string FilePathTrimmed { get; } = "No BSP loaded";

    [ObservableAsProperty]
    public string? PakfileSize { get; }

    public StatusBarViewModel()
    {
        IObservable<BspFile?> bsp = BspService.Instance.WhenAnyValue(x => x.BspFile);

        bsp.Select(x =>
            {
                string? path = x?.FilePath;
                if (path is null)
                    return "No BSP loaded";

                const int max = 40;
                return path.Length > max ? $"...{path[^max..]}" : path;
            })
            .ToPropertyEx(this, x => x.FilePathTrimmed);

        bsp.Select(x =>
            {
                PakfileLump? pakfile = x?.GetLump<PakfileLump>();
                if (pakfile is null)
                    return null;

                // pakfile.DataStreamLength

                // Zip.TotalSize is not a reliable source of compressed sized, since
                // when we write the zip out compressed we're just writing to a stream
                // straight out to disk, not modifying the zip in memory in any way.
                // DataStreamLength *is* an accurate value for the size though, either
                // compressed or uncompressed.
                long inMemorySize = pakfile.DataStreamLength;
                long uncompressedSize = pakfile.Zip.TotalUncompressSize;

                string str = $"{FileSizeConverter.FormattedFileSize(inMemorySize)} Pakfile";
                return inMemorySize != uncompressedSize
                    ? str + $" ({FileSizeConverter.FormattedFileSize(uncompressedSize)} uncompressed)"
                    : str;
            })
            .ToPropertyEx(this, x => x.PakfileSize);
    }
}
