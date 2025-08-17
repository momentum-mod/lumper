namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Vtf;
using Lumper.UI.Views.Shared;
using Lumper.UI.Views.Shared.Pakfile;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using VTFLib;

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

    [ObservableAsProperty]
    public bool IsAnimated { get; }

    [Reactive]
    public uint SelectedResizeHeight { get; set; } = 512;

    [Reactive]
    public uint SelectedResizeWidth { get; set; } = 512;

    public uint[] ResizeOptions { get; } = [16, 32, 64, 128, 256, 512, 1024, 2048, 4096];

    [Reactive]
    public VTFImageFormat SelectedImageFormat { get; set; }

    public VTFImageFormat[] ImageFormats { get; } = Enum.GetValues<VTFImageFormat>();

    public bool HasSeparateWindow { get; set; }

    public PakfileEntryVtfViewModel(PakfileEntry entry, BspNode parent)
        : base(entry, parent)
    {
        RegisterView<PakfileEntryVtfViewModel, PakfileEntryVtfView>();

        // Ugly null handling, see
        // https://www.reactiveui.net/docs/handbook/when-any.html#null-propogation-inside-whenanyvalue
        this.WhenAnyValue(x => x.VtfFile, x => x.VtfFile!.FaceCount, (file, count) => file is not null ? count - 1 : 0)
            .ToPropertyEx(this, x => x.FaceMax);

        this.WhenAnyValue(x => x.VtfFile, x => x.VtfFile!.FrameCount, (file, count) => file is not null ? count - 1 : 0)
            .ToPropertyEx(this, x => x.FrameMax);

        this.WhenAnyValue(
                x => x.VtfFile,
                x => x.VtfFile!.MipmapCount,
                (file, count) => file is not null ? count - 1 : 0
            )
            .ToPropertyEx(this, x => x.MipmapMax);

        this.WhenAnyValue(x => x.MipmapLevel, x => x.Frame, x => x.Face, x => x.Slice)
            .Skip(1)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(x => _ = FetchImage());

        this.WhenAnyValue(x => x.VtfFile, x => x.VtfFile!.FrameCount, (file, count) => file is not null && count > 1)
            .ToPropertyEx(this, x => x.IsAnimated);

        this.WhenAnyValue(x => x.VtfFile, x => x.VtfFile!.ImageFormat).Subscribe(x => SelectedImageFormat = x.Item2);
    }

    private bool _inited;

    public override void Load(CancellationTokenSource? cts = null)
    {
        if (Loaded || (cts?.IsCancellationRequested ?? false))
            return;

        if (!_inited)
        {
            VtfFile ??= new VtfFileViewModel(this);
            _inited = true;
        }

        _ = FetchImage(cts);
    }

    private async Task FetchImage(CancellationTokenSource? cts = null)
    {
        if (VtfFile is null || (cts?.IsCancellationRequested ?? false))
            return;

        Image<Rgba32>? image = await VtfFile.GetImage(cts, Frame, Face, Slice, MipmapLevel);
        if (image is null || (cts?.IsCancellationRequested ?? false))
            return;

        Loaded = true;
        Image = ImageToBitmap(image);
    }

    public async Task SetImage(bool createNew)
    {
        if (VtfFile is null)
            return;

        IReadOnlyList<IStorageFile> result = await Program.MainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Pick image file",
                FileTypeFilter = GenerateImageFileFilter(),
            }
        );
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

    public async Task Resize()
    {
        if (VtfFile is null)
            return;

        TexDataLump? texDataLump = BspService.Instance.BspFile?.GetLump<TexDataLump>();
        TexInfoLump? texInfoLump = BspService.Instance.BspFile?.GetLump<TexInfoLump>();
        if (texDataLump is null || texInfoLump is null)
            return;

        string name = Regex.Replace(Key, @"^materials/|\.vtf$", "", RegexOptions.IgnoreCase);

        float widthScaleFactor = SelectedResizeHeight / (float)VtfFile.ImageHeight;
        float heightScaleFactor = SelectedResizeWidth / (float)VtfFile.ImageWidth;

        await VtfFile.ResizeImage(SelectedResizeWidth, SelectedResizeHeight, Frame, Face, Slice, MipmapLevel);
        await FetchImage();

        foreach (TexData texData in texDataLump.Data)
        {
            if (!texData.TexName.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            texData.Width = (int)SelectedResizeWidth;
            texData.Height = (int)SelectedResizeHeight;
            // Honestly not sure what this does, and judging by engine code, *nor does Valve*.
            // VBSP sets it to the same as Width/Height, so we'll do the same.
            texData.ViewWidth = (int)SelectedResizeWidth;
            texData.ViewHeight = (int)SelectedResizeHeight;

            foreach (TexInfo texInfo in texInfoLump.Data)
            {
                if (texInfo.TexDataPointer != texData.StringTablePointer)
                    continue;

                for (int i = 0; i < 4; i++)
                {
                    texInfo.TextureVectors[0, i] *= widthScaleFactor;
                    texInfo.TextureVectors[1, i] *= heightScaleFactor;
                }
            }
        }
    }

    public async Task Reencode()
    {
        if (VtfFile is null)
            return;

        await VtfFile.Reencode(SelectedImageFormat, Frame, Face, Slice, MipmapLevel);
        await FetchImage();
    }

    public void OpenVtfImageWindow()
    {
        if (HasSeparateWindow)
            return;

        var window = new VtfImageWindow
        {
            DataContext = this,
            Height = 1024,
            Width = 1288,
        }; // 1024 + 256 + 8
        window.Show();

        HasSeparateWindow = true;
    }

    private static Bitmap ImageToBitmap(Image<Rgba32> image)
    {
        using var mem = new MemoryStream();
        image.SaveAsBmp(mem, Encoder);
        mem.Seek(0, SeekOrigin.Begin);
        return new Bitmap(mem);
    }

    private static readonly BmpEncoder Encoder = new()
    {
        SupportTransparency = true,
        BitsPerPixel = BmpBitsPerPixel.Pixel32,
        SkipMetadata = false,
    };

    private static FilePickerFileType[] GenerateImageFileFilter()
    {
        return
        [
            new FilePickerFileType("Image files") { Patterns = ["*.bmp", "*.jpeg", "*.jpg", "*.png"] },
            new FilePickerFileType("All files") { Patterns = ["*"] },
        ];
    }
}
