namespace Lumper.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
///     Format percentage string
/// </summary>
public class PercentConverter : IValueConverter
{
    public static PercentConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return $"{(int)System.Convert.ToDouble(value, CultureInfo.InvariantCulture)}%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
