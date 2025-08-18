namespace Lumper.UI.ViewModels.Shared.Vtf;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Lumper.UI.ViewModels;
using NLog;
using Pakfile;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using VTFLib;

/// <summary>
/// This is a simple reactive wrapper around VTFLib.NET (which is itself a wrapper around VTFLib).
/// </summary>
public class VtfFileViewModel(PakfileEntryVtfViewModel pakfileEntry) : ViewModel
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

    static VtfFileViewModel()
    {
        if (VTFAPI.Initialize())
            Logger.Debug("Initialized VTFAPI");
        else
            Logger.Error("Failed to initialize VTFAPI!");
    }

    public async Task<Image<Rgba32>?> GetImage(
        CancellationTokenSource? cts,
        uint frame,
        uint face,
        uint slice,
        uint mipmapLevel
    )
    {
        byte[]? data = await VtfLibQueue.Run<byte[]>(
            () =>
            {
                Prepare();

                if (VTFFile.ImageGetHasImage() == 0)
                    return null!;

                nint imageData = VTFFile.ImageGetData(frame, face, slice, mipmapLevel);
                int imageSize = (int)VTFFile.ImageComputeImageSize(ImageWidth, ImageHeight, 1, 1, ImageFormat);

                byte[] data = new byte[imageSize];
                Marshal.Copy(imageData, data, 0, imageSize);

                if (imageSize <= 0)
                {
                    Logger.Error("Image data array size is 0");
                    return null!;
                }

                return data;
            },
            cts
        );

        if (data is null)
            return null;

        byte[] dest = new byte[ImageWidth * ImageHeight * 4];
        VTFFile.ImageConvertToRGBA8888(data, dest, ImageWidth, ImageHeight, ImageFormat);

        var rgba = new Rgba32[ImageWidth * ImageHeight];
        int j = 0;
        for (int i = 0; i < dest.Length; i += 4)
            rgba[j++] = new Rgba32(dest[i], dest[i + 1], dest[i + 2], dest[i + 3]);

        // We could make the UI significantly faster if we did a resize here. But code is complicated and leads
        // to doing *more* work doing the heaviest part of the loading process, so not doing for now.
        // Worth considering if we work on VTF Browser perf in the future.
        return Image.LoadPixelData<Rgba32>(rgba.AsSpan(), (int)ImageWidth, (int)ImageHeight);
    }

    public async Task SetImageData(Image<Rgba32> image, uint frame, uint face, uint slice, uint mipmapLevel)
    {
        await VtfLibQueue.Run(() =>
        {
            Prepare();

            byte[] imageRgbaData = GetRgba8888FromImage(image, out _);

            int size = (int)VTFFile.ImageComputeImageSize((uint)image.Width, (uint)image.Height, 1, 1, ImageFormat);
            byte[] vtfImageData = new byte[size];

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
    }

    public async Task SetNewImage(Image<Rgba32> image)
    {
        await VtfLibQueue.Run(() =>
        {
            Prepare();

            byte[] buffer = GetRgba8888FromImage(image, out bool hasAlpha);
            var createOptions = new SVTFCreateOptions();
            VTFFile.ImageCreateDefaultCreateStructure(ref createOptions);

            // TODO: Allow picking this. For Strata-based games, use BC7!
            createOptions.imageFormat = hasAlpha ? VTFImageFormat.IMAGE_FORMAT_DXT5 : VTFImageFormat.IMAGE_FORMAT_DXT1;

            if (!VTFFile.ImageCreateSingle((uint)image.Width, (uint)image.Height, buffer, ref createOptions))
            {
                try
                {
                    string err = VTFAPI.GetLastError();
                    Logger.Warn($"Error updating VTF ${pakfileEntry.Key}: ${err}");
                }
                catch (Win32Exception ex)
                {
                    Logger.Error(ex, "fucking vtflib!!");
                }
            }

            SaveVtf();
        });
    }

    public async Task ResizeImage(uint newWidth, uint newHeight, uint frame, uint face, uint slice, uint mipmapLevel)
    {
        if (newWidth <= 0 || (newWidth & (newWidth - 1)) != 0 || newHeight <= 0 || (newHeight & (newHeight - 1)) != 0)
            throw new ArgumentException("Sizes must be powers of 2");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // This is really stupid but don't want to spend the time transforming VTFLib queueing stuff to use a priority
        // queue -- .NET doesn't have one built-in, and my attempt to hack something together was too buggy
        // and gross to justify. We'll remove all that crap when we move to vtfpp anyway!
        Logger.Info(
            $"Resizing {pakfileEntry.Key} from {ImageWidth}x{ImageHeight} to {newWidth}x{newHeight}... NOTE this operation won't start if the texture browser is currently loading textures!!"
        );

        Image<Rgba32>? image = await GetImage(cts: null, frame, face, slice, mipmapLevel);
        if (image is null)
        {
            Logger.Error($"Resizing {pakfileEntry.Key}: Failed to get image for resizing");
            return;
        }

        Logger.Info($"Resizing {pakfileEntry.Key}: Fetched VTF image data ({stopwatch.ElapsedMilliseconds}ms)");

        image.Mutate(x => x.Resize((int)newWidth, (int)newHeight, new BicubicResampler()));
        byte[] resizedData = GetRgba8888FromImage(image, out bool _);

        Logger.Info(
            $"Resizing {pakfileEntry.Key}: Resized raw image data in memory ({stopwatch.ElapsedMilliseconds}ms)"
        );

        await VtfLibQueue.Run(() =>
        {
            Prepare();

            SVTFCreateOptions createOptions = GetCreateOptionsWithExistingValues();

            if (!VTFFile.ImageCreateSingle(newWidth, newHeight, resizedData, ref createOptions))
            {
                string err = VTFAPI.GetLastError();
                Logger.Error($"Resizing {pakfileEntry.Key}: Error creating new VTF during VTF creation: ${err}");
                return;
            }

            Logger.Info(
                $"Resizing {pakfileEntry.Key}: Created resized VTF file in memory ({stopwatch.ElapsedMilliseconds}ms)"
            );

            SaveVtf();

            Logger.Info(
                $"Resizing {pakfileEntry.Key}: Updated VTF file in pakfile lump. Resizing complete! ({stopwatch.ElapsedMilliseconds}ms)"
            );
        });
    }

    public async Task Reencode(VTFImageFormat newFormat, uint frame, uint face, uint slice, uint mipmapLevel)
    {
        if (newFormat == ImageFormat)
        {
            Logger.Warn($"Re-encoding {pakfileEntry.Key} to the same format, skipping.");
            return;
        }

        Logger.Info(
            $"Reencoding {pakfileEntry.Key} to {GetImageFormatString(newFormat)}... NOTE this operation won't start if the texture browser is currently loading textures!!"
        );

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        Image<Rgba32>? image = await GetImage(cts: null, frame, face, slice, mipmapLevel);
        if (image is null)
        {
            Logger.Error($"Reencoding {pakfileEntry.Key}: Failed to get image for resizing");
            return;
        }

        byte[] oldData = GetRgba8888FromImage(image, out bool _);

        await VtfLibQueue.Run(() =>
        {
            Prepare();

            // Note: VTFLib has a ImageConvert function specifically for this, but I can't get it to work.
            // If I copy the existing data from the VTF file and call ImageConvert, it returns true,
            // but nothing is updated. Instead just reading image data to RGBA8888 and creating a new VTF
            // from scratch.

            SVTFCreateOptions createOptions = GetCreateOptionsWithExistingValues();
            createOptions.imageFormat = newFormat;

            if (!VTFFile.ImageCreateSingle(ImageWidth, ImageHeight, oldData, ref createOptions))
            {
                string err = VTFAPI.GetLastError();
                Logger.Error($"Reencoding {pakfileEntry.Key}: Error creating new VTF during VTF creation: ${err}");
                return;
            }

            Logger.Info(
                $"Reencoding {pakfileEntry.Key}: Created resized VTF file in memory ({stopwatch.ElapsedMilliseconds}ms)"
            );

            SaveVtf();

            Logger.Info(
                $"Reencoding {pakfileEntry.Key}: Updated VTF file in pakfile lump. Resizing complete! ({stopwatch.ElapsedMilliseconds}ms)"
            );
        });
    }

    private void Prepare()
    {
        if (Loaded)
        {
            VTFFile.BindImage(_imageIndex);
            return;
        }

        // Copy (ToArray) is required since VTFLib needs a byte[] which is inherently mutable.
        byte[] vtfBuffer = pakfileEntry.GetData().ToArray();

        VTFFile.CreateImage(ref _imageIndex);
        VTFFile.BindImage(_imageIndex);
        VTFFile.ImageLoadLump(vtfBuffer, (uint)vtfBuffer.Length, false);

        Update();
        Loaded = true;
    }

    private void SaveVtf()
    {
        VTFFile.BindImage(_imageIndex);
        uint size = VTFFile.ImageGetSize();

        byte[] vtfBuffer = new byte[size];
        uint uiSize = 0;
        if (!VTFFile.ImageSaveLump(vtfBuffer, (uint)vtfBuffer.Length, ref uiSize))
        {
            string err = VTFAPI.GetLastError();
            Logger.Error($"Error saving VTF ${pakfileEntry.Key}: ${err}");
        }

        pakfileEntry.UpdateData(vtfBuffer);
        Update();
    }

    private void Update()
    {
        FrameCount = VTFFile.ImageGetFrameCount();
        FaceCount = VTFFile.ImageGetFaceCount();
        MipmapCount = VTFFile.ImageGetMipmapCount();
        Flags = (VTFImageFlag)VTFFile.ImageGetFlags();
        FlagList = Utils.ExpandBitfield(Flags).Select(x => x.ToString().Replace("TEXTUREFLAGS_", "")).ToList();
        MajorVersion = VTFFile.ImageGetMajorVersion();
        MinorVersion = VTFFile.ImageGetMinorVersion();
        ImageSize = VTFFile.ImageGetSize();
        ImageWidth = VTFFile.ImageGetWidth();
        ImageHeight = VTFFile.ImageGetHeight();
        ImageFormat = VTFFile.ImageGetFormat();
        ImageFormatString = GetImageFormatString(ImageFormat);
        Depth = VTFFile.ImageGetDepth();

        float x = 0,
            y = 0,
            z = 0;
        VTFFile.ImageGetReflectivity(ref x, ref y, ref z);
        Reflectivity = [Math.Round(x, 2), Math.Round(y, 2), Math.Round(z, 2)];
    }

    public static string GetImageFormatString(VTFImageFormat format)
    {
        return format.ToString().Replace("IMAGE_FORMAT_", "");
    }

    private static byte[] GetRgba8888FromImage(Image<Rgba32> image, out bool hasAlpha)
    {
        int size = image.Width * image.Height * 4;
        using var mem = new MemoryStream();
        byte[] buffer = new byte[size];
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

    private static SVTFCreateOptions GetCreateOptionsWithExistingValues()
    {
        float r = 0;
        float g = 0;
        float b = 0;
        VTFFile.ImageGetReflectivity(ref r, ref g, ref b);
        var createOptions = new SVTFCreateOptions();
        VTFFile.ImageCreateDefaultCreateStructure(ref createOptions);
        createOptions.versionMajor = VTFFile.ImageGetMajorVersion();
        createOptions.versionMinor = VTFFile.ImageGetMinorVersion();
        createOptions.imageFormat = VTFFile.ImageGetFormat();
        createOptions.flags = VTFFile.ImageGetFlags();
        createOptions.startFrame = VTFFile.ImageGetStartFrame();
        createOptions.bumpScale = VTFFile.ImageGetBumpmapScale();
        createOptions.reflectivityR = r;
        createOptions.reflectivityG = g;
        createOptions.reflectivityB = b;
        createOptions.mipmaps = VTFFile.ImageGetMipmapCount() > 1;

        return createOptions;
    }

    /// <summary>
    /// VTFLib operates on a single VTF file at a time.
    /// The class wraps VTFLib-based operations in a thread-safe queue that processes all
    /// the VTFLib stuff in a single thread.
    /// </summary>
    private static class VtfLibQueue
    {
        private static readonly ConcurrentQueue<(Func<object>, CancellationTokenSource?)> Queue = new();
        private static readonly Subject<(Func<object>, object?)> Output = new();
        private static readonly Lock Lock = new();
        private static bool _isRunning;

        public static Task Run(Action fn)
        {
            return Run(fn, null);
        }

        public static async Task Run(Action fn, CancellationTokenSource? cts)
        {
            await Run<object>(
                () =>
                {
                    fn();
                    return true; // Return literally anything so this is a valid Func<object>
                },
                cts
            );
        }

        public static Task<T> Run<T>(Func<object> fn)
        {
            return Run<T>(fn, null)!;
        }

        public static async Task<T?> Run<T>(Func<object> fn, CancellationTokenSource? cts)
        {
            Queue.Enqueue((fn, cts));

            lock (Lock)
            {
                if (!_isRunning)
                {
                    _isRunning = true;
                    Observable.Start(ProcessQueue, RxApp.TaskpoolScheduler);
                }
            }

            return await Output.FirstAsync(x => x.Item1 == fn).Select(x => (T?)x.Item2);
        }

        private static void ProcessQueue()
        {
            while (!Queue.IsEmpty)
            {
                (Func<object>, CancellationTokenSource?) next;
                while (!Queue.TryDequeue(out next)) { }

                (Func<object> fn, CancellationTokenSource? cts) = next;

                if (cts?.IsCancellationRequested ?? false)
                {
                    Output.OnNext((fn, null));
                    continue;
                }

                object result = fn();
                Output.OnNext((fn, result));
            }

            _isRunning = false;
        }
    }
}
