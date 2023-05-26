using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SharpCompress.Archives.Zip;
using ReactiveUI;
using VTFLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

public class PakFileEntryVtfViewModel : PakFileEntryLeafViewModel
{
    public PakFileEntryVtfViewModel(PakFileEntryBranchViewModel parent,
        ZipArchiveEntry entry, string name)
        : base(parent, entry, name)
    { }
    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"PakFileEntry{(string.IsNullOrWhiteSpace(_name) ? "" : $" ({_name})")}";

    private string _info = "";
    //todo remove this later and add seperate properties instead
    public string Info
    {
        get => _info;
        set => this.RaiseAndSetIfChanged(ref _info, value);
    }
    public Image? _image = null;
    public Image? Image
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }

    public override void Open()
    {
        base.Open();
        VTFAPI.Initialize();
        //todo don't save to file first
        string fileName = "tmp.vtf";
        using var file = File.Open(fileName, FileMode.Create);
        Stream.CopyTo(file);

        uint image = 0;
        VTFFile.CreateImage(ref image);
        VTFFile.BindImage(image);
        VTFFile.ImageLoad(fileName, false);

        Info = $"MajorVersion: {VTFFile.ImageGetMajorVersion()}\n" +
                  $"MinorVersion: {VTFFile.ImageGetMinorVersion()}\n" +
                  $"Size: {VTFFile.ImageGetSize()}\n" +
                  $"Width: {VTFFile.ImageGetWidth()}\n" +
                  $"Height: {VTFFile.ImageGetHeight()}\n" +
                  $"Format: {Enum.GetName(VTFFile.ImageGetFormat())}\n";

        /*if (VTFFile.ImageGetHasThumbnail())
        {
            uint w = VTFFile.ImageGetThumbnailWidth();
            uint h = VTFFile.ImageGetThumbnailHeight();
            var f = VTFFile.ImageGetThumbnailFormat();
            var ucharPtr = VTFFile.ImageGetThumbnailData();
            var img = GetImage(ucharPtr, w, h, f);
            //img.SaveAsBmp("thumbnail.bmp");
        }*/
        uint hasImage = VTFFile.ImageGetHasImage();
        if (hasImage != 0)
        {
            uint w = VTFFile.ImageGetWidth();
            uint h = VTFFile.ImageGetHeight();
            var f = VTFFile.ImageGetFormat();
            var ucharPtr = VTFFile.ImageGetData(0, 0, 0, 0);
            var img = GetImage(ucharPtr, w, h, f);
            //img.SaveAsBmp("tmp.bmp");
            Image = img;
        }
    }

    private Image GetImage(IntPtr ptr, uint width, uint height, VTFImageFormat format)
    {
        int size = (int)width * (int)height * sizeof(byte) * 4;
        var data = new byte[size];

        GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
        IntPtr pointer = pinnedArray.AddrOfPinnedObject();

        VTFFile.ImageConvertToRGBA8888(ptr, pointer, width, height, format);
        Marshal.Copy(pointer, data, 0, size);

        var img = GetImageFromRgba8888(data, (int)width, (int)height);

        pinnedArray.Free();
        return img;
    }
    private Image GetImageFromRgba8888(byte[] img, int width, int height)
    {
        var asdf = new Rgba32[width * height];
        int j = 0;
        for (int i = 0; i < img.Length; i += 4)
        {
            asdf[j++] = new Rgba32(
                img[i],
                img[i + 1],
                img[i + 2],
                img[i + 3]);
        }

        return Image.LoadPixelData<Rgba32>(asdf.AsSpan(), width, height);
    }
}
