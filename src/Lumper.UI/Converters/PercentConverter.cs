namespace Lumper.UI.Converters;
using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

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
        var d = System.Convert.ToDouble(value);
        return $"{(int)d}%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture) => new BindingNotification(
            new ArgumentOutOfRangeException(nameof(value)),
            BindingErrorType.DataValidationError);
}
