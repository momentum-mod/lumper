using System;
using System.IO;
using System.Runtime.InteropServices;
using ReactiveUI;
using VTFLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

public class PakFileEntryVtfViewModel : PakFileEntryLeafViewModel
{
    public PakFileEntryVtfViewModel(PakFileEntryBranchViewModel parent,
        PakFileEntry entry, string name)
        : base(parent, entry, name)
    {
    }

    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"PakFileEntry{(string.IsNullOrWhiteSpace(Name) ? "" : $" ({Name})")}";

    private string _info = "";
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

    private uint _frame;
    public uint Frame
    {
        get => _frame;
        private set
        {
            this.RaiseAndSetIfChanged(ref _frame, value);
            OpenImage();
        }
    }

    public uint _face;
    public uint Face
    {
        get => _face;
        private set
        {
            this.RaiseAndSetIfChanged(ref _face, value);
            OpenImage();
        }
    }

    public uint _slice;
    public uint Slice
    {
        get => _slice;
        private set
        {
            this.RaiseAndSetIfChanged(ref _slice, value);
            OpenImage();
        }
    }

    public uint _mipmapLevel;
    public uint MipmapLevel
    {
        get => _mipmapLevel;
        private set
        {
            this.RaiseAndSetIfChanged(ref _mipmapLevel, value);
            OpenImage();
        }
    }

    private uint _depth;
    public uint Depth
    {
        get => _depth;
        private set => this.RaiseAndSetIfChanged(ref _depth, value);
    }

    private uint _frameCount;
    public uint FrameCount
    {
        get => _frameCount;
        private set
        {
            if (_frameCount != value)
            {
                _frameCount = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(FrameMax));
            }
        }
    }
    public uint FrameMax { get { return FrameCount - 1; } }

    private uint _faceCount;
    public uint FaceCount
    {
        get => _faceCount;
        private set
        {
            if (_faceCount != value)
            {
                _faceCount = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(FaceMax));
            }
        }
    }
    public uint FaceMax { get { return FaceCount - 1; } }

    private uint _mipmapCount;
    public uint MipmapCount
    {
        get => _mipmapCount;
        private set
        {
            if (_mipmapCount != value)
            {
                _mipmapCount = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(MipmapMax));
            }
        }
    }
    public uint MipmapMax { get { return MipmapCount - 1; } }

    public VTFImageFlag _flags;
    public VTFImageFlag Flags
    {
        get => _flags;
        private set => this.RaiseAndSetIfChanged(ref _flags, value);
    }

    private uint imageIndex = 0;
    protected bool Opened { get; private set; }
    public override void Open()
    {
        VTFAPI.Initialize();
        //can't get length for byte array from LzmaStream 
        //so we need to read to a different stream first
        using var mem = new MemoryStream();
        _entry.DataStream.CopyTo(mem);
        byte[] vtfBuffer = mem.ToArray();

        if (!Opened)
            VTFFile.CreateImage(ref imageIndex);
        else
            Opened = true;
        VTFFile.BindImage(imageIndex);
        VTFFile.ImageLoadLump(vtfBuffer, (uint)vtfBuffer.Length, false);

        Depth = VTFFile.ImageGetDepth();

        FrameCount = VTFFile.ImageGetFrameCount();
        FaceCount = VTFFile.ImageGetFaceCount();
        MipmapCount = VTFFile.ImageGetMipmapCount();
        Flags = (VTFImageFlag)VTFFile.ImageGetFlags();

        Info = $"MajorVersion: {VTFFile.ImageGetMajorVersion()}\n" +
                  $"MinorVersion: {VTFFile.ImageGetMinorVersion()}\n" +
                  $"Size: {VTFFile.ImageGetSize()}\n" +
                  $"Width: {VTFFile.ImageGetWidth()}\n" +
                  $"Height: {VTFFile.ImageGetHeight()}\n" +
                  $"Format: {Enum.GetName(VTFFile.ImageGetFormat())}\n" +
                  $"Depth: {Depth}\n" +
                  $"FrameCount: {FrameCount}\n" +
                  $"FaceCount: {FaceCount}\n" +
                  $"MipmapCount: {MipmapCount}\n" +
                  $"Flags: {Flags.ToString().Replace(",", "\n")}\n";

        OpenImage();
    }

    private void OpenImage()
    {
        VTFFile.BindImage(imageIndex);
        uint hasImage = VTFFile.ImageGetHasImage();
        if (hasImage != 0)
        {
            uint w = VTFFile.ImageGetWidth();
            uint h = VTFFile.ImageGetHeight();
            var f = VTFFile.ImageGetFormat();
            IntPtr ucharPtr = VTFFile.ImageGetData(Frame, Face, Slice, MipmapLevel);
            var size = (int)VTFFile.ImageComputeImageSize(w, h, 1, 1, f);
            var img = GetImage(ucharPtr, size, w, h, f);
            Image = img;
        }
    }
    private Image<Rgba32> GetImage(IntPtr ptr, int size, uint width, uint height, VTFImageFormat format)
    {
        var data = new byte[size];
        Marshal.Copy(ptr, data, 0, size);
        return GetImage(data, width, height, format);
    }
    private Image<Rgba32> GetImage(byte[] source, uint width, uint height, VTFImageFormat format)
    {
        int size = (int)width * (int)height * 4;
        if (size <= 0)
            throw new ArgumentException("image data array size is 0");
        var dest = new byte[size];
        VTFFile.ImageConvertToRGBA8888(source, dest, width, height, format);
        var img = GetImageFromRgba8888(dest, (int)width, (int)height);
        return img;
    }

    public static Image<Rgba32> ImageFromFileStream(Stream fileSteam)
    {
        return SixLabors.ImageSharp.Image.Load<Rgba32>(fileSteam);
    }

    public void SetImageData(Image<Rgba32> image)
    {
        _isModified = true;
        VTFFile.BindImage(imageIndex);
        byte[] buffer = GetRgba888FromImage(image, out _);

        var f = VTFFile.ImageGetFormat();
        int size = (int)VTFFile.ImageComputeImageSize(
            (uint)image.Width, (uint)image.Height, 1, 1, f);
        var buffer2 = new byte[size];
        VTFFile.ImageConvertFromRGBA8888(
            buffer,
            buffer2,
            (uint)image.Width,
            (uint)image.Height,
            f
            );
        VTFFile.ImageSetData(Frame, Face, Slice, MipmapLevel, buffer2);
        SaveVTF();
    }

    public void SetNewImage(Image<Rgba32> image)
    {
        _isModified = true;
        byte[] buffer = GetRgba888FromImage(image, out bool hasAlpha);
        var createOptions = new SVTFCreateOptions();
        VTFFile.BindImage(imageIndex);
        VTFFile.ImageCreateDefaultCreateStructure(ref createOptions);
        createOptions.imageFormat = hasAlpha ?
                                    VTFImageFormat.IMAGE_FORMAT_DXT5 :
                                    VTFImageFormat.IMAGE_FORMAT_DXT1;
        if (!VTFFile.ImageCreateSingle(
            (uint)image.Width,
            (uint)image.Height,
            buffer,
            ref createOptions))
        {
            string err = VTFAPI.GetLastError();
            Console.WriteLine(err);
        }

        SaveVTF();
    }

    public void SaveVTF()
    {
        var vtfBuffer = new byte[VTFFile.ImageGetSize()];
        _entry.DataStream = new MemoryStream(vtfBuffer);

        uint uiSize = 0;
        if (!VTFFile.ImageSaveLump(vtfBuffer, (uint)vtfBuffer.Length, ref uiSize))
        {
            string err = VTFAPI.GetLastError();
            Console.WriteLine(err);
        }
        _entry.DataStream.Seek(0, SeekOrigin.Begin);
    }

    private static Image<Rgba32> GetImageFromRgba8888(byte[] img, int width, int height)
    {
        var rgba = new Rgba32[width * height];
        int j = 0;
        for (int i = 0; i < img.Length; i += 4)
        {
            rgba[j++] = new Rgba32(
                img[i],
                img[i + 1],
                img[i + 2],
                img[i + 3]);
        }

        return SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(rgba.AsSpan(), width, height);
    }

    private static byte[] GetRgba888FromImage(Image<Rgba32> image, out bool hasAlpha)
    {
        int size = image.Width * image.Height * 4;
        using var mem = new MemoryStream();
        var buffer = new byte[size];
        int i = 0;
        hasAlpha = false;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgba32 pixel = image[x, y];
                buffer[i++] = pixel.R;
                buffer[i++] = pixel.G;
                buffer[i++] = pixel.B;
                buffer[i++] = pixel.A;
                if (!hasAlpha && pixel.A != 255)
                    hasAlpha = true;
            }
        }
        return buffer;
    }

    public override void Update()
    {
        _isModified = false;
        base.Update();
    }
}
