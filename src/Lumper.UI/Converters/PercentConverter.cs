using System;
using System.Globalization;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Lumper.UI.Converters;

/// <summary>
///     format percentage string
/// </summary>
public class PercentConverter : IValueConverter
{
    public static PercentConverter Instance
    {
        get;
    } = new();

    public object Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        double d = System.Convert.ToDouble(value);
        return $"{(int)d}%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        return new BindingNotification(
            new ArgumentOutOfRangeException(nameof(value)),
            BindingErrorType.DataValidationError);
    }
}
