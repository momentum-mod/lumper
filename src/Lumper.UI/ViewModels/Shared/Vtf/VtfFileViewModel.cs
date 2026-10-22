namespace Lumper.UI.ViewModels.Shared.Vtf;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Lumper.UI.ViewModels;
using NLog;
using Pakfile;
using ReactiveUI.Fody.Helpers;
using sourcepp.vtfpp;

/// <summary>
/// This is a simple reactive wrapper around vtfpp (via the sourcepp C# bindings).
///
/// Unlike VTFLib, vtfpp has no global "bound image" state, so we don't hold onto any native handles: each operation
/// parses the VTF from the pakfile entry into its own <see cref="VTF"/>, does its work, and disposes it.
/// </summary>
public partial class VtfFileViewModel(PakfileEntryVtfViewModel pakfileEntry) : ViewModel
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
    public ImageFormat ImageFormat { get; private set; }

    [Reactive]
    public string ImageFormatString { get; private set; } = "";

    [Reactive]
    public VTF.Flags Flags { get; private set; }

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

    /// <summary>
    /// Formats offered when re-encoding. This is the set of formats that are actually useful in a map; vtfpp supports
    /// plenty more (console formats, virtual HDR formats, etc.) that Source's map compile/loading doesn't care about.
    /// </summary>
    public static readonly ImageFormat[] SelectableImageFormats =
    [
        ImageFormat.RGBA8888,
        ImageFormat.ABGR8888,
        ImageFormat.RGB888,
        ImageFormat.BGR888,
        ImageFormat.RGB565,
        ImageFormat.I8,
        ImageFormat.IA88,
        ImageFormat.A8,
        ImageFormat.ARGB8888,
        ImageFormat.BGRA8888,
        ImageFormat.DXT1,
        ImageFormat.DXT3,
        ImageFormat.DXT5,
        ImageFormat.BGRX8888,
        ImageFormat.BGR565,
        ImageFormat.BGRX5551,
        ImageFormat.BGRA4444,
        ImageFormat.DXT1_ONE_BIT_ALPHA,
        ImageFormat.BGRA5551,
        ImageFormat.UV88,
        ImageFormat.UVWQ8888,
        ImageFormat.RGBA16161616F,
        ImageFormat.RGBA16161616,
        ImageFormat.ATI2N,
        ImageFormat.ATI1N,
        ImageFormat.STRATA_BC7,
        ImageFormat.STRATA_BC6H_SF,
        ImageFormat.STRATA_BC6H_UF,
        ImageFormat.STRATA_BC5,
        ImageFormat.STRATA_BC4,
    ];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Catmull-Rom is what ImageSharp calls "bicubic", which is what we used to resize with.
    private const ImageConversion.ResizeFilter DefaultResizeFilter = ImageConversion.ResizeFilter.CATMULL_ROM;

    // Reads don't need this, since they always work on their own copy of the VTF, but we don't want two edits to the
    // same entry racing and one clobbering the other. Edits are user-triggered, so one lock for everything is fine.
    private static readonly SemaphoreSlim EditLock = new(1, 1);

    public Task<Bitmap?> GetImage(
        CancellationTokenSource? cts,
        uint frame,
        uint face,
        uint slice,
        uint mipmapLevel
    )
    {
        return Task.Run<Bitmap?>(() =>
        {
            if (cts?.IsCancellationRequested ?? false)
                return null;

            using VTF? vtf = Open();
            if (vtf is null || !vtf.HasImageData)
                return null;

            var mip = (byte)mipmapLevel;
            ushort width = vtf.Width(mip);
            ushort height = vtf.Height(mip);

            // vtfpp's conversion of HDR data to 8 bits comes out too dark, so we do it ourselves
            byte[] rgba =
                vtf.Format == ImageFormat.RGBA16161616F
                    ? HdrImageConversion.Rgba16161616FToRgba8888(
                        vtf.GetImageDataRaw(mip, (ushort)frame, (byte)face, (ushort)slice),
                        width * height
                    )
                    : vtf.GetImageDataAsRGBA8888(mip, (ushort)frame, (byte)face, (ushort)slice);
            if (width == 0 || height == 0 || rgba.Length < width * height * 4)
            {
                Logger.Error($"Failed to get image data for {pakfileEntry.Key}");
                return null;
            }

            // We could make the UI significantly faster if we did a resize here. But code is complicated and leads
            // to doing *more* work doing the heaviest part of the loading process, so not doing for now.
            // Worth considering if we work on VTF Browser perf in the future.
            return RgbaToBitmap(rgba, width, height);
        });
    }

    /// <summary>
    /// Replace the entire VTF with the given image file, which can be any format vtfpp can decode, or another VTF.
    /// </summary>
    public Task<bool> SetNewImage(byte[] file, bool isVtf)
    {
        return Edit(() =>
        {
            if (isVtf)
            {
                // Drop in the VTF as-is rather than re-baking it, we don't want to change anything about it.
                using var newVtf = new VTF(file);
                if (!newVtf)
                {
                    Logger.Error($"Updating {pakfileEntry.Key}: file is not a valid VTF");
                    return false;
                }

                pakfileEntry.UpdateData(file);
                Refresh(newVtf, file.Length);
                return true;
            }

            if (!TryDecodeImage(file, out byte[] rgba, out ushort width, out ushort height))
                return false;

            using VTF? current = Open();
            if (current is null)
                return false;

            // TODO: Allow picking the format. For Strata-based games, use BC7!
            var options = new VTF.CreationOptions
            {
                Version = current.Version,
                OutputFormat = HasAlpha(rgba) ? ImageFormat.DXT5 : ImageFormat.DXT1,
            };

            using VTF? vtf = VTF.Create(rgba, ImageFormat.RGBA8888, width, height, options);
            if (vtf is null || !vtf)
            {
                Logger.Error($"Updating {pakfileEntry.Key}: failed to create VTF from image");
                return false;
            }

            return Save(vtf);
        });
    }

    /// <summary>
    /// Replace a single frame/face/slice/mipmap of the VTF with the given image file, which can be any format vtfpp
    /// can decode, or another VTF (in which case its first image is used).
    /// </summary>
    public Task<bool> SetImageData(byte[] file, bool isVtf, uint frame, uint face, uint slice, uint mipmapLevel)
    {
        return Edit(() =>
        {
            byte[] rgba;
            ushort width;
            ushort height;
            if (isVtf)
            {
                using var source = new VTF(file);
                if (!source || !source.HasImageData)
                {
                    Logger.Error($"Updating {pakfileEntry.Key}: file is not a valid VTF");
                    return false;
                }

                width = source.Width();
                height = source.Height();
                rgba = source.GetImageDataAsRGBA8888();
            }
            else if (!TryDecodeImage(file, out rgba, out width, out height))
            {
                return false;
            }

            using VTF? vtf = Open();
            if (vtf is null)
                return false;

            var mip = (byte)mipmapLevel;
            // vtfpp resizes and converts the image to match the VTF
            if (!vtf.SetImage(rgba, ImageFormat.RGBA8888, width, height, DefaultResizeFilter, mip, (ushort)frame, (byte)face, (ushort)slice))
            {
                Logger.Error($"Updating {pakfileEntry.Key}: failed to set image data");
                return false;
            }

            // Smaller mips are now stale, and nobody is going to want to edit those by hand.
            if (mip == 0 && vtf.MipCount > 1)
                vtf.ComputeMips(DefaultResizeFilter);

            return Save(vtf);
        });
    }

    public async Task ResizeImage(uint newWidth, uint newHeight)
    {
        if (newWidth <= 0 || (newWidth & (newWidth - 1)) != 0 || newHeight <= 0 || (newHeight & (newHeight - 1)) != 0)
            throw new ArgumentException("Sizes must be powers of 2");

        bool success = await Edit(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Info($"Resizing {pakfileEntry.Key} to {newWidth}x{newHeight}...");

            using VTF? vtf = Open();
            if (vtf is null)
                return false;

            // Resizes every mip, frame and face
            vtf.SetSize((ushort)newWidth, (ushort)newHeight, DefaultResizeFilter);

            if (!Save(vtf))
                return false;

            Logger.Info($"Resizing {pakfileEntry.Key}: complete! ({stopwatch.ElapsedMilliseconds}ms)");
            return true;
        });

        if (!success)
            Logger.Error($"Resizing {pakfileEntry.Key} failed");
    }

    public async Task Reencode(ImageFormat newFormat)
    {
        if (newFormat == ImageFormat)
        {
            Logger.Warn($"Re-encoding {pakfileEntry.Key} to the same format, skipping.");
            return;
        }

        bool success = await Edit(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Info($"Re-encoding {pakfileEntry.Key} to {newFormat}...");

            using VTF? vtf = Open();
            if (vtf is null)
                return false;

            // Strata's formats are only understood by VTF version 7.6 and up
            if (newFormat is >= ImageFormat.STRATA_R8 and <= ImageFormat.STRATA_BC4 && vtf.Version < 6)
            {
                Logger.Info($"Re-encoding {pakfileEntry.Key}: upgrading VTF version to 7.6 for {newFormat}");
                vtf.Version = 6;
            }

            vtf.SetFormat(newFormat);
            if (vtf.Format != newFormat)
            {
                Logger.Error($"Re-encoding {pakfileEntry.Key}: vtfpp was unable to convert to {newFormat}");
                return false;
            }

            if (!Save(vtf))
                return false;

            Logger.Info($"Re-encoding {pakfileEntry.Key}: complete! ({stopwatch.ElapsedMilliseconds}ms)");
            return true;
        });

        if (!success)
            Logger.Error($"Re-encoding {pakfileEntry.Key} failed");
    }

    private async Task<bool> Edit(Func<bool> edit)
    {
        await EditLock.WaitAsync();
        try
        {
            return await Task.Run(edit);
        }
        finally
        {
            EditLock.Release();
        }
    }

    /// <summary>
    /// Parses the VTF held by the pakfile entry. The caller owns the returned VTF and must dispose it.
    /// </summary>
    private VTF? Open()
    {
        ReadOnlySpan<byte> data = pakfileEntry.GetData();
        var vtf = new VTF(data);
        if (!vtf)
        {
            Logger.Error($"Failed to parse VTF {pakfileEntry.Key}");
            vtf.Dispose();
            return null;
        }

        if (!Loaded)
            Refresh(vtf, data.Length);

        return vtf;
    }

    private bool Save(VTF vtf)
    {
        byte[] data = vtf.Bake();
        if (data.Length == 0)
        {
            Logger.Error($"Error saving VTF {pakfileEntry.Key}");
            return false;
        }

        pakfileEntry.UpdateData(data);
        Refresh(vtf, data.Length);
        return true;
    }

    private void Refresh(VTF vtf, int fileSize)
    {
        FrameCount = vtf.FrameCount;
        FaceCount = vtf.FaceCount;
        MipmapCount = vtf.MipCount;
        Flags = vtf.VtfFlags;
        FlagList = GetFlagNames(vtf.VtfFlags, vtf.Version);
        // vtfpp's version is just the minor version, there's only ever been one major version
        MajorVersion = 7;
        MinorVersion = vtf.Version;
        ImageSize = (uint)fileSize;
        ImageWidth = vtf.Width();
        ImageHeight = vtf.Height();
        ImageFormat = vtf.Format;
        ImageFormatString = ImageFormat.ToString();
        Depth = vtf.Depth();

        (float x, float y, float z) = vtf.Reflectivity;
        Reflectivity = [Math.Round(x, 2), Math.Round(y, 2), Math.Round(z, 2)];

        Loaded = true;
    }

    /// <summary>
    /// Decode any image file supported by vtfpp (PNG, JPEG, TGA, BMP, WebP, HDR, EXR, ...) to RGBA8888.
    /// </summary>
    private bool TryDecodeImage(byte[] file, out byte[] rgba, out ushort width, out ushort height)
    {
        rgba = [];
        width = 0;
        height = 0;

        var format = ImageFormat.EMPTY;
        int w = 0,
            h = 0,
            frames = 0;
        byte[] data = ImageConversion.ConvertFileToImageData(file, ref format, ref w, ref h, ref frames);
        if (data.Length == 0 || format == ImageFormat.EMPTY || w <= 0 || h <= 0 || frames <= 0)
        {
            Logger.Error($"Updating {pakfileEntry.Key}: unable to read image file");
            return false;
        }

        if (w > ushort.MaxValue || h > ushort.MaxValue)
        {
            Logger.Error($"Updating {pakfileEntry.Key}: image is too large ({w}x{h}), VTFs are limited to 65535px");
            return false;
        }

        // Animated images come back as every frame in one buffer, we only want the first.
        if (frames > 1)
            data = data[..(data.Length / frames)];

        width = (ushort)w;
        height = (ushort)h;
        rgba =
            format == ImageFormat.RGBA8888
                ? data
                : ImageConversion.ConvertImageDataToFormat(data, format, ImageFormat.RGBA8888, width, height);
        return true;
    }

    private static bool HasAlpha(byte[] rgba)
    {
        for (int i = 3; i < rgba.Length; i += 4)
        {
            if (rgba[i] != 255)
                return true;
        }

        return false;
    }

    private static WriteableBitmap RgbaToBitmap(byte[] rgba, int width, int height)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Unpremul
        );

        using ILockedFramebuffer framebuffer = bitmap.Lock();
        int rowBytes = width * 4;
        if (framebuffer.RowBytes == rowBytes)
        {
            Marshal.Copy(rgba, 0, framebuffer.Address, rowBytes * height);
        }
        else
        {
            for (int y = 0; y < height; y++)
                Marshal.Copy(rgba, y * rowBytes, framebuffer.Address + (y * framebuffer.RowBytes), rowBytes);
        }

        return bitmap;
    }

    private static readonly (string Name, uint Value)[] AllFlags = Enum.GetNames<VTF.Flags>()
        .Where(x => !x.StartsWith("MASK_", StringComparison.Ordinal))
        .Select(x => (x, (uint)Enum.Parse<VTF.Flags>(x)))
        .ToArray();

    [GeneratedRegex(@"^V\d_")]
    private static partial Regex FlagVersionPrefix();

    /// <summary>
    /// vtfpp's flags enum covers every VTF version, and the same bit means different things in different versions, so
    /// we can't just expand the bitfield. Work out which names apply to this version, preferring newer names.
    /// </summary>
    private static List<string> GetFlagNames(VTF.Flags flags, uint version)
    {
        var names = new List<(uint Value, string Name)>();
        uint claimed = 0;

        foreach (string prefix in GetFlagPrefixes(version))
        {
            uint groupBits = 0;
            foreach ((string name, uint value) in AllFlags.Where(x => x.Name.StartsWith(prefix, StringComparison.Ordinal)))
            {
                if ((claimed & value) != 0)
                    continue;

                groupBits |= value;

                // The VTEX flags are only meaningful to Valve's vtex tool, and never end up in a compiled VTF.
                if (((uint)flags & value) != 0 && !name.Contains("VTEX", StringComparison.Ordinal))
                    names.Add((value, FlagVersionPrefix().Replace(name, "")));
            }

            claimed |= groupBits;
        }

        return names.OrderBy(x => x.Value).Select(x => x.Name).ToList();
    }

    // Newest first
    private static IEnumerable<string> GetFlagPrefixes(uint version)
    {
        if (version >= 6)
            yield return "V6_";

        if (version >= 5)
        {
            yield return "V5_CSGO_";
            yield return "V5_";
        }
        else if (version >= 4)
        {
            yield return "V4_TF2_";
            yield return "V4_";
        }

        if (version >= 3)
            yield return "V3_";

        if (version >= 2)
            yield return "V2_";

        if (version >= 1)
            yield return "V1_";

        yield return "V0_";
    }
}
