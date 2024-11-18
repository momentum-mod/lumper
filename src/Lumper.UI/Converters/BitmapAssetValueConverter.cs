namespace Lumper.UI.Converters;

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Converts a string path to a bitmap asset.
/// The asset must be in the same assembly as the program. If it isn't,
/// specify "avares://Lumper.UI/" in front of the path to the asset.
///
/// NOTE: Unused currently as it's more performant to just convert VTFs to bitmaps immediately.
/// Didn't want to delete entirely, could be useful in future, eh.
/// </summary>
public class BitmapAssetValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case string rawUri when targetType.IsAssignableFrom(typeof(Bitmap)):
            {
                Uri uri;

                // Allow for assembly overrides
                if (rawUri.StartsWith("avares://"))
                {
                    uri = new Uri(rawUri);
                }
                else
                {
                    string? assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
                    if (assemblyName is null)
                    {
                        return new BindingNotification(
                            new ArgumentNullException(nameof(value)),
                            BindingErrorType.DataValidationError
                        );
                    }

                    uri = new Uri($"avares://{assemblyName}{rawUri}");
                }

                return new Bitmap(AssetLoader.Open(uri));
            }
            case Image<Rgba32> img when targetType.IsAssignableFrom(typeof(Bitmap)):
            {
                using var mem = new MemoryStream();
                var encoder = new BmpEncoder
                {
                    SupportTransparency = true,
                    BitsPerPixel = BmpBitsPerPixel.Pixel32,
                    SkipMetadata = false,
                };
                img.SaveAsBmp(mem, encoder);
                mem.Seek(0, SeekOrigin.Begin);
                return new Bitmap(mem);
            }
            default:
                // Return null so FallbackValue can handle? Not sure this is right, could do a dedicated image.
                return null!;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        new BindingNotification(new ArgumentOutOfRangeException(nameof(value)), BindingErrorType.DataValidationError);
}
