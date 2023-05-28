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
    private bool _isModified = false;
    public override bool IsModified { get => _isModified; }
    public Image<Rgba32>? _image = null;
    public Image<Rgba32>? Image
    {
        get => _image;
        private set => this.RaiseAndSetIfChanged(ref _image, value);
    }

    public override void Open()
    {
        base.Open();
        VTFAPI.Initialize();
        var reader = new BinaryReader(Stream);
        byte[] vtfBuffer = reader.ReadBytes((int)Stream.Length);

        uint image = 0;
        VTFFile.CreateImage(ref image);
        VTFFile.BindImage(image);
        VTFFile.ImageLoadLump(vtfBuffer, (uint)vtfBuffer.Length, false);

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
            //var ucharPtr = VTFFile.ImageGetData(0, 0, 0, 0);
            var ucharPtr = VTFFile.ImageGetData(1, 1, 1, 1);
            var img = GetImage(ucharPtr, w, h, f);
            //img.SaveAsBmp("tmp.bmp");
            Image = img;
        }
    }

    private Image<Rgba32> GetImage(IntPtr ptr, uint width, uint height, VTFImageFormat format)
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

    //todo meh
    public static Image<Rgba32> ImageFromFileStream(Stream fileSteam)
    {
        return SixLabors.ImageSharp.Image.Load<Rgba32>(fileSteam);
    }
    public void SetImage(Image<Rgba32> image)
    {
        Image = image;
        _isModified = true;
    }
    public void SaveImage()
    {
        if (_image != null)
        {
            int size = _image.Width * _image.Height * 4;
            using var mem = new MemoryStream();
            var buffer = new byte[size];
            int i = 0;
            for (int y = 0; y < _image.Height; y++)
            {
                for (int x = 0; x < _image.Width; x++)
                {
                    Rgba32 pixel = _image[x, y];
                    buffer[i++] = pixel.R;
                    buffer[i++] = pixel.G;
                    buffer[i++] = pixel.B;
                    buffer[i++] = pixel.A;
                }
            }



            var createOptions = new SVTFCreateOptions();

            VTFFile.ImageCreateDefaultCreateStructure(ref createOptions);
            createOptions.imageFormat = VTFImageFormat.IMAGE_FORMAT_DXT5;
            if (!VTFFile.ImageCreateSingle(
                (uint)_image.Width,
                (uint)_image.Height,
                buffer,
                ref createOptions))
            {
                string err = VTFAPI.GetLastError();
                Console.WriteLine(err);
            }

            //todo where do I dispose this
            var vtfBuffer = new byte[VTFFile.ImageGetSize()];
            Stream = new MemoryStream(vtfBuffer);

            uint uiSize = 0;
            VTFFile.ImageSaveLump(vtfBuffer, (uint)vtfBuffer.Length, ref uiSize);
        }
    }
    /*private void SetImage(byte[] data)
    {
        VTFFile.ImageSetData(0, 0, 0, 0, data);
    }*/
    private Image<Rgba32> GetImageFromRgba8888(byte[] img, int width, int height)
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

        return SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(asdf.AsSpan(), width, height);
    }

    public override void Save(ZipArchive zip)
    {
        if (IsModified)
        {
            SaveImage();
            Stream.Seek(0, SeekOrigin.Begin);
            _isModified = false;
        }
        else
            Stream = _entry.OpenEntryStream();
        base.Save(zip);
    }
}
