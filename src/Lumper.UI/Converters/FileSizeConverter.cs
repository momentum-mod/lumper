namespace Lumper.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
///     Format file size in bytes with rounding and KB/bytes suffix
///
///     I'm copying the behaviour of windows explorer here, which I like.
///     Showing files greater than 1000 KB in terms of KB rather than MB/GB/etc...
///     is more readable, and pakfile items will rarely exceed 10mb or so.
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public static FileSizeConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        double convertedSize = System.Convert.ToDouble(value);

        return $"{Math.Ceiling(convertedSize / 1024 * 10) / 10:N1} KB";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
