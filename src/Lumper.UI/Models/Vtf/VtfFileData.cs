namespace Lumper.UI.Models;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Lumper.Lib.BSP.Struct;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VTFLib;

public class VtfFileData
{
    private readonly PakFileEntry _entry;
    private readonly uint _imageIndex;

    public uint FrameCount { get; private set; }
    public uint FaceCount { get; private set; }
    public uint MipmapCount { get; private set; }
    public uint Depth { get; private set; }
    public VTFImageFlag Flags { get; private set; }
    public uint MajorVersion { get; private set; }
    public uint MinorVersion { get; private set; }
    public uint ImageSize { get; private set; }
    public uint ImageWidth { get; private set; }
    public uint ImageHeight { get; private set; }
    public VTFImageFormat ImageFormat { get; private set; }

    static VtfFileData() => VTFAPI.Initialize();
    public VtfFileData(PakFileEntry entry)
    {
        _entry = entry;
        using var mem = new MemoryStream();
        _entry.DataStream.CopyTo(mem);
        var vtfBuffer = mem.ToArray();

        VTFFile.CreateImage(ref _imageIndex);

        VTFFile.BindImage(_imageIndex);
        VTFFile.ImageLoadLump(vtfBuffer, (uint)vtfBuffer.Length, false);

        Update();
    }

    public Image<Rgba32>? GetImage(uint frame, uint face, uint slice, uint mipmapLevel)
    {
        VTFFile.BindImage(_imageIndex);

        if (VTFFile.ImageGetHasImage() == 0)
        {
            return null;
        }

        var imageData = VTFFile.ImageGetData(frame, face, slice, mipmapLevel);
        var imageSize = (int)VTFFile.ImageComputeImageSize(ImageWidth, ImageHeight, 1, 1, ImageFormat);

        var data = new byte[imageSize];
        Marshal.Copy(imageData, data, 0, imageSize);

        if (imageSize <= 0)
            throw new ArgumentException("image data array size is 0");
        var dest = new byte[ImageWidth * ImageHeight * 4];
        VTFFile.ImageConvertToRGBA8888(data, dest, ImageWidth, ImageHeight, ImageFormat);

        var rgba = new Rgba32[ImageWidth * ImageHeight];
        var j = 0;
        for (var i = 0; i < dest.Length; i += 4)
        {
            rgba[j++] = new Rgba32(
                dest[i],
                dest[i + 1],
                dest[i + 2],
                dest[i + 3]);
        }

        return Image.LoadPixelData<Rgba32>(rgba.AsSpan(), (int)ImageWidth, (int)ImageHeight);
    }

    public void SetImageData(Image<Rgba32> image, uint frame, uint face, uint slice, uint mipmapLevel)
    {
        VTFFile.BindImage(_imageIndex);
        var imageRgbaData = GetRgba8888FromImage(image, out _);

        var size = (int)VTFFile.ImageComputeImageSize(
            (uint)image.Width, (uint)image.Height, 1, 1, ImageFormat);
        var vtfImageData = new byte[size];

        VTFFile.ImageConvertFromRGBA8888(
            imageRgbaData,
            vtfImageData,
            (uint)image.Width,
            (uint)image.Height,
            ImageFormat
        );

        VTFFile.ImageSetData(frame, face, slice, mipmapLevel, vtfImageData);
        SaveVtf();
    }

    public void SetNewImage(Image<Rgba32> image)
    {
        VTFFile.BindImage(_imageIndex);
        var buffer = GetRgba8888FromImage(image, out var hasAlpha);
        var createOptions = new SVTFCreateOptions();
        VTFFile.ImageCreateDefaultCreateStructure(ref createOptions);
        createOptions.imageFormat =
            hasAlpha ? VTFImageFormat.IMAGE_FORMAT_DXT5 : VTFImageFormat.IMAGE_FORMAT_DXT1;
        if (!VTFFile.ImageCreateSingle(
                (uint)image.Width,
                (uint)image.Height,
                buffer,
                ref createOptions))
        {
            var err = VTFAPI.GetLastError();
            Console.WriteLine(err);
        }

        SaveVtf();
    }

    private void SaveVtf()
    {
        var size = VTFFile.ImageGetSize();

        var vtfBuffer = new byte[size];
        _entry.DataStream = new MemoryStream(vtfBuffer);

        uint uiSize = 0;
        if (!VTFFile.ImageSaveLump(vtfBuffer, (uint)vtfBuffer.Length, ref uiSize))
        {
            var err = VTFAPI.GetLastError();
            Console.WriteLine(err);
        }

        _entry.DataStream.Seek(0, SeekOrigin.Begin);
        Update();
    }

    private void Update()
    {
        Depth = VTFFile.ImageGetDepth();
        FrameCount = VTFFile.ImageGetFrameCount();
        FaceCount = VTFFile.ImageGetFaceCount();
        MipmapCount = VTFFile.ImageGetMipmapCount();
        Flags = (VTFImageFlag)VTFFile.ImageGetFlags();
        MajorVersion = VTFFile.ImageGetMajorVersion();
        MinorVersion = VTFFile.ImageGetMinorVersion();
        ImageSize = VTFFile.ImageGetSize();
        ImageWidth = VTFFile.ImageGetWidth();
        ImageHeight = VTFFile.ImageGetHeight();
        ImageFormat = VTFFile.ImageGetFormat();
    }

    private static byte[] GetRgba8888FromImage(Image<Rgba32> image, out bool hasAlpha)
    {
        var size = image.Width * image.Height * 4;
        using var mem = new MemoryStream();
        var buffer = new byte[size];
        var i = 0;
        hasAlpha = false;
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
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
}
