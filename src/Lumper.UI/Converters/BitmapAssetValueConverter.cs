using System;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Lumper.UI.Converters;

/// <summary>
///     <para>
///         Converts a string path to a bitmap asset.
///     </para>
///     <para>
///         The asset must be in the same assembly as the program. If it isn't,
///         specify "avares://Lumper.UI/" in front of the path to the asset.
///     </para>
/// </summary>
public class BitmapAssetValueConverter : IValueConverter
{
    public static BitmapAssetValueConverter Instance
    {
        get;
    } = new();

    public object Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        switch (value)
        {
            case null:
                return new BindingNotification(
                    new ArgumentNullException(nameof(value)),
                    BindingErrorType.DataValidationError);
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
                    string? assemblyName =
                        Assembly.GetEntryAssembly()?.GetName().Name;
                    if (assemblyName is null)
                        return new BindingNotification(
                            new ArgumentNullException(nameof(assemblyName)),
                            BindingErrorType.DataValidationError);

                    uri = new Uri($"avares://{assemblyName}{rawUri}");
                }

                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                if (assets is null)
                    return new BindingNotification(
                        new ArgumentNullException(nameof(assets)),
                        BindingErrorType.DataValidationError);

                var asset = assets.Open(uri);
                return new Bitmap(asset);
            }
            default:
                return new BindingNotification(
                    new ArgumentOutOfRangeException(nameof(value)),
                    BindingErrorType.DataValidationError);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        return new BindingNotification(
            new ArgumentOutOfRangeException(nameof(value)),
            BindingErrorType.DataValidationError);
    }
}
