using System;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VTFLib;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.Models;

public class VtfFileData
{
    private readonly PakFileEntry _entry;
    private readonly uint _imageIndex;

    private uint _frameCount;
    private uint _faceCount;
    private uint _mipmapCount;
    private uint _depth;
    private VTFImageFlag _flags;
    private uint _majorVersion;
    private uint _minorVersion;
    private uint _imageSize;
    private uint _imageWidth;
    private uint _imageHeight;
    private VTFImageFormat _imageFormat;

    public uint FrameCount => _frameCount;
    public uint FaceCount => _faceCount;
    public uint MipmapCount => _mipmapCount;
    public uint Depth => _depth;
    public VTFImageFlag Flags => _flags;
    public uint MajorVersion => _majorVersion;
    public uint MinorVersion => _minorVersion;
    public uint ImageSize => _imageSize;
    public uint ImageWidth => _imageWidth;
    public uint ImageHeight => _imageHeight;
    public VTFImageFormat ImageFormat => _imageFormat;

    static VtfFileData()
    {
        VTFAPI.Initialize();
    }
    public VtfFileData(PakFileEntry entry)
    {
        _entry = entry;
        using var mem = new MemoryStream();
        _entry.DataStream.CopyTo(mem);
        byte[] vtfBuffer = mem.ToArray();

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

        IntPtr imageData = VTFFile.ImageGetData(frame, face, slice, mipmapLevel);
        int imageSize = (int)VTFFile.ImageComputeImageSize(_imageWidth, _imageHeight, 1, 1, _imageFormat);

        byte[] data = new byte[imageSize];
        Marshal.Copy(imageData, data, 0, imageSize);

        if (imageSize <= 0)
            throw new ArgumentException("image data array size is 0");
        byte[] dest = new byte[_imageWidth * _imageHeight * 4];
        VTFFile.ImageConvertToRGBA8888(data, dest, _imageWidth, _imageHeight, _imageFormat);

        var rgba = new Rgba32[_imageWidth * _imageHeight];
        int j = 0;
        for(int i = 0; i < dest.Length; i += 4)
        {
            rgba[j++] = new Rgba32(
                dest[i],
                dest[i + 1],
                dest[i + 2],
                dest[i + 3]);
        }

        return Image.LoadPixelData<Rgba32>(rgba.AsSpan(), (int)_imageWidth, (int)_imageHeight);
    }

    public void SetImageData(Image<Rgba32> image, uint frame, uint face, uint slice, uint mipmapLevel)
    {
        VTFFile.BindImage(_imageIndex);
        byte[] imageRgbaData = GetRgba8888FromImage(image, out _);

        int size = (int)VTFFile.ImageComputeImageSize(
            (uint)image.Width, (uint)image.Height, 1, 1, _imageFormat);
        byte[] vtfImageData = new byte[size];

        VTFFile.ImageConvertFromRGBA8888(
            imageRgbaData,
            vtfImageData,
            (uint)image.Width,
            (uint)image.Height,
            _imageFormat
        );

        VTFFile.ImageSetData(frame, face, slice, mipmapLevel, vtfImageData);
        SaveVtf();
    }

    public void SetNewImage(Image<Rgba32> image)
    {
        VTFFile.BindImage(_imageIndex);
        byte[] buffer = GetRgba8888FromImage(image, out bool hasAlpha);
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
            string err = VTFAPI.GetLastError();
            Console.WriteLine(err);
        }

        SaveVtf();
    }

    private void SaveVtf()
    {
        uint size = VTFFile.ImageGetSize();

        var vtfBuffer = new byte[size];
        _entry.DataStream = new MemoryStream(vtfBuffer);

        uint uiSize = 0;
        if (!VTFFile.ImageSaveLump(vtfBuffer, (uint)vtfBuffer.Length, ref uiSize))
        {
            string err = VTFAPI.GetLastError();
            Console.WriteLine(err);
        }

        _entry.DataStream.Seek(0, SeekOrigin.Begin);
        Update();
    }

    private void Update()
    {
        _depth = VTFFile.ImageGetDepth();
        _frameCount = VTFFile.ImageGetFrameCount();
        _faceCount = VTFFile.ImageGetFaceCount();
        _mipmapCount = VTFFile.ImageGetMipmapCount();
        _flags = (VTFImageFlag)VTFFile.ImageGetFlags();
        _majorVersion = VTFFile.ImageGetMajorVersion();
        _minorVersion = VTFFile.ImageGetMinorVersion();
        _imageSize = VTFFile.ImageGetSize();
        _imageWidth = VTFFile.ImageGetWidth();
        _imageHeight = VTFFile.ImageGetHeight();
        _imageFormat = VTFFile.ImageGetFormat();
    }

    private static byte[] GetRgba8888FromImage(Image<Rgba32> image, out bool hasAlpha)
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
}
