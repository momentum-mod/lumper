namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Lib.BSP.Struct;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using Vtf;

public class PakfileEntryVtfViewModel : PakfileEntryViewModel
{
    [Reactive]
    public VtfFileViewModel? VtfFile { get; private set; }

    [Reactive]
    public bool Loaded { get; private set; }

    [Reactive]
    public Bitmap? Image { get; set; }

    [Reactive]
    public uint Frame { get; set; }

    [Reactive]
    public uint Face { get; set; }

    [Reactive]
    public uint Slice { get; set; }

    [Reactive]
    public uint MipmapLevel { get; set; }

    [ObservableAsProperty]
    public uint FrameMax { get; }

    [ObservableAsProperty]
    public uint FaceMax { get; }

    [ObservableAsProperty]
    public uint MipmapMax { get; }

    private static Lazy<Bitmap> LazyMissingImage { get; } = new(()
        => new Bitmap(AssetLoader.Open(new Uri("avares://Lumper.UI/Assets/Images/TextureLoadError.png"))));

    public Bitmap MissingImage => LazyMissingImage.Value;

    public PakfileEntryVtfViewModel(PakfileEntry entry) : base(entry)
    {
        // Ugly null handling, see
        // https://www.reactiveui.net/docs/handbook/when-any.html#null-propogation-inside-whenanyvalue
        this.WhenAnyValue(
                x => x.VtfFile,
                x => x.VtfFile!.FaceCount,
                (file, count) => file is not null ? count - 1 : 0)
            .ToPropertyEx(this, x => x.FaceMax);

        this.WhenAnyValue(
                x => x.VtfFile,
                x => x.VtfFile!.FrameCount,
                (file, count) => file is not null ? count - 1 : 0)
            .ToPropertyEx(this, x => x.FrameMax);

        this.WhenAnyValue(
                x => x.VtfFile,
                x => x.VtfFile!.MipmapCount,
                (file, count) => file is not null ? count - 1 : 0)
            .ToPropertyEx(this, x => x.MipmapMax);

        this.WhenAnyValue(
                x => x.MipmapLevel,
                x => x.Frame,
                x => x.Face,
                x => x.Slice)
            .Skip(1)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(x => _ = FetchImage());
    }

    private bool _inited;

    public override void Load()
    {
        if (_inited)
            return;
        _inited = true;

        VtfFile ??= new VtfFileViewModel(BaseEntry);
        _ = FetchImage();
    }


    private async Task FetchImage()
    {
        if (VtfFile is null)
            return;

        Image<Rgba32>? image = await VtfFile.GetImage(Frame, Face, Slice, MipmapLevel);
        if (image is null)
            return;

        Loaded = true;
        Image = ImageToBitmap(image);
    }

    public async Task SetImage(bool createNew)
    {
        if (VtfFile is null || Program.Desktop.MainWindow is null)
            return;

        IReadOnlyList<IStorageFile> result =
            await Program.Desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
                AllowMultiple = false, Title = "Pick image file", FileTypeFilter = GenerateImageFileFilter()
            });
        if (result is not { Count: 1 })
            return;

        var image = SixLabors.ImageSharp.Image.Load<Rgba32>(await result[0].OpenReadAsync());

        IsModified = true;
        if (createNew)
            await VtfFile.SetNewImage(image);
        else
            await VtfFile.SetImageData(image, Frame, Face, Slice, MipmapLevel);

        MarkAsModified();
        await FetchImage();
    }

    // Doesn't actually need to do any work since SetNewImage writes to PakfileEntry stream
    public override void UpdateModel() { }

    private static Bitmap ImageToBitmap(Image<Rgba32> image)
    {
        using var mem = new MemoryStream();
        image.SaveAsBmp(mem, Encoder);
        mem.Seek(0, SeekOrigin.Begin);
        return new Bitmap(mem);
    }

    private static readonly BmpEncoder Encoder = new() {
        SupportTransparency = true, BitsPerPixel = BmpBitsPerPixel.Pixel32, SkipMetadata = false
    };

    private static FilePickerFileType[] GenerateImageFileFilter() => [
        new FilePickerFileType("Image files") { Patterns = new[] { "*.bmp", "*.jpeg", "*.jpg", "*.png" } },
        new FilePickerFileType("All files") { Patterns = new[] { "*" } }
    ];
}
