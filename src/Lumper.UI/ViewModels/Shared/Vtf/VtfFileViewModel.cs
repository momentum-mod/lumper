namespace Lumper.UI.ViewModels.Shared.Vtf;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Lib.BSP.Struct;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ViewModels;
using VTFLib;

/// <summary>
/// This is a simple reactive wrapper around VTFLib.NET (which is itself a wrapper around VTFLib).
/// </summary>
public class VtfFileViewModel(PakfileEntry pakfileEntry) : ViewModel
{
    [Reactive]
    public bool Loaded { get; private set; }

    [Reactive]
    public uint MajorVersion { get; private set; }

    [Reactive]
    public uint MinorVersion { get; private set; }

    [Reactive]
    public uint ImageSize { get; private set; }

    [Reactive]
    public uint ImageWidth { get; private set; }

    [Reactive]
    public uint ImageHeight { get; private set; }

    [Reactive]
    public VTFImageFormat ImageFormat { get; private set; }

    [Reactive]
    public string ImageFormatString { get; private set; } = "";

    [Reactive]
    public VTFImageFlag Flags { get; private set; }

    [Reactive]
    public List<string> FlagList { get; private set; } = [];

    [Reactive]
    public uint FrameCount { get; private set; }

    [Reactive]
    public uint FaceCount { get; private set; }

    [Reactive]
    public uint MipmapCount { get; private set; }

    [Reactive]
    public uint Depth { get; private set; }

    [Reactive]
    public double[] Reflectivity { get; private set; } = null!;

    private uint _imageIndex;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static VtfFileViewModel() => VTFAPI.Initialize();

    public async Task<Image<Rgba32>?> GetImage(uint frame, uint face, uint slice, uint mipmapLevel)
        => await VtfLibQueue.Run<Image<Rgba32>?>(() =>
        {
            Prepare();

            if (VTFFile.ImageGetHasImage() == 0)
                return null!;

            var imageData = VTFFile.ImageGetData(frame, face, slice, mipmapLevel);
            var imageSize = (int)VTFFile.ImageComputeImageSize(ImageWidth, ImageHeight, 1, 1, ImageFormat);

            var data = new byte[imageSize];
            Marshal.Copy(imageData, data, 0, imageSize);

            if (imageSize <= 0)
            {
                Logger.Error("Image data array size is 0");
                return null!;
            }

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
        });

    public async Task SetImageData(Image<Rgba32> image, uint frame, uint face, uint slice, uint mipmapLevel)
        => await VtfLibQueue.Run(() =>
        {
            Prepare();

            var imageRgbaData = GetRgba8888FromImage(image, out _);

            var size = (int)VTFFile.ImageComputeImageSize((uint)image.Width, (uint)image.Height, 1, 1, ImageFormat);
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
        });

    public async Task SetNewImage(Image<Rgba32> image)
        => await VtfLibQueue.Run(() =>
        {
            Prepare();

            var buffer = GetRgba8888FromImage(image, out var hasAlpha);
            var createOptions = new SVTFCreateOptions();
            VTFFile.ImageCreateDefaultCreateStructure(ref createOptions);

            // TODO: Allow picking this. For Strata-based games, use BC7!
            createOptions.imageFormat =
                hasAlpha ? VTFImageFormat.IMAGE_FORMAT_DXT5 : VTFImageFormat.IMAGE_FORMAT_DXT1;
            if (!VTFFile.ImageCreateSingle((uint)image.Width, (uint)image.Height, buffer, ref createOptions))
            {
                try
                {
                    var err = VTFAPI.GetLastError();
                    Logger.Warn($"Error updating VTF ${pakfileEntry.Key}: ${err}");
                }
                catch (Win32Exception ex)
                {
                    Logger.Error(ex, "fucking vtflib!!");
                }
            }

            SaveVtf();
        });


    private void Prepare()
    {
        if (Loaded)
        {
            VTFFile.BindImage(_imageIndex);
            return;
        }

        using var mem = new MemoryStream();
        // Would love to avoid a copy here but can't figure it out, don't want
        // to expose byte array from PakfileLump and couldn't get ReadonlyMemory<byte> to work.
        pakfileEntry.GetReadOnlyStream().CopyTo(mem);
        var vtfBuffer = mem.GetBuffer();

        VTFFile.CreateImage(ref _imageIndex);
        VTFFile.BindImage(_imageIndex);
        VTFFile.ImageLoadLump(vtfBuffer, (uint)vtfBuffer.Length, false);

        Update();
        Loaded = true;
    }

    private void SaveVtf()
    {
        VTFFile.BindImage(_imageIndex);
        var size = VTFFile.ImageGetSize();

        var vtfBuffer = new byte[size];
        pakfileEntry.UpdateData(vtfBuffer);

        uint uiSize = 0;
        if (!VTFFile.ImageSaveLump(vtfBuffer, (uint)vtfBuffer.Length, ref uiSize))
        {
            var err = VTFAPI.GetLastError();
            Logger.Error($"Error saving VTF ${pakfileEntry.Key}: ${err}");
        }

        Update();
    }

    private void Update()
    {
        FrameCount = VTFFile.ImageGetFrameCount();
        FaceCount = VTFFile.ImageGetFaceCount();
        MipmapCount = VTFFile.ImageGetMipmapCount();
        Flags = (VTFImageFlag)VTFFile.ImageGetFlags();
        FlagList = Utils.ExpandBitfield(Flags)
            .Select(x => x.ToString().Replace("TEXTUREFLAGS_", ""))
            .ToList();
        MajorVersion = VTFFile.ImageGetMajorVersion();
        MinorVersion = VTFFile.ImageGetMinorVersion();
        ImageSize = VTFFile.ImageGetSize();
        ImageWidth = VTFFile.ImageGetWidth();
        ImageHeight = VTFFile.ImageGetHeight();
        ImageFormat = VTFFile.ImageGetFormat();
        ImageFormatString = ImageFormat.ToString().Replace("IMAGE_FORMAT_", "");
        Depth = VTFFile.ImageGetDepth();

        float x = 0, y = 0, z = 0;
        VTFFile.ImageGetReflectivity(ref x, ref y, ref z);
        Reflectivity = [Math.Round(x, 2), Math.Round(y, 2), Math.Round(z, 2)];
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

    /// <summary>
    /// VTFLib operates on a single VTF file at a time.
    /// The class wraps VTFLib-based operations in a thread-safe queue that processes all
    /// the VTFLib stuff in a single thread.
    /// </summary>
    private static class VtfLibQueue // @formatter:off
    {
        private static readonly ConcurrentQueue<Func<object>> Queue = new();
        private static readonly Subject<(Func<object>, object)> Output = new();
        private static readonly object Lock = new();
        private static bool _isRunning;

        public static async Task Run(Action fn)
            => await Run<object>(() => { fn(); return true; });

        public static async Task<T> Run<T>(Func<object> fn)
        {
            Queue.Enqueue(fn);

            lock (Lock)
            {
                if (!_isRunning)
                {
                    _isRunning = true;
                    Observable.Start(ProcessQueue, RxApp.TaskpoolScheduler);
                }
            }

            return await Output
                .FirstAsync(x => x.Item1 == fn)
                .Select(x => (T)x.Item2);
        }

        private static void ProcessQueue()
        {
            while (!Queue.IsEmpty)
            {
                Func<object>? next;
                while (!Queue.TryDequeue(out next))
                { }
                var result = next();
                Output.OnNext((next, result));
            }

            _isRunning = false;
        }
    }
}
