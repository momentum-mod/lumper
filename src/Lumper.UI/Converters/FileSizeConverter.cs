namespace Lumper.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
/// Format file size in bytes with rounding and GB/MB/.../KB/bytes suffix
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public static FileSizeConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        double convertedSize = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);

        return FormattedFileSize(convertedSize);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public static string FormattedFileSize(double size)
    {
        return size switch
        {
            < 1024 => $"{size:N0} bytes",
            < 1024 * 1024 => $"{Math.Ceiling(size / 1024 * 10) / 10:N1} KB",
            < 1024 * 1024 * 1024 => $"{Math.Ceiling(size / 1024 / 1024 * 10) / 10:N1} MB",
            _ => $"{Math.Ceiling(size / 1024 / 1024 / 1024 * 10) / 10:N1} GB",
        };
    }

    public static string FormattedFileSize(int size)
    {
        return FormattedFileSize((double)size);
    }
}
