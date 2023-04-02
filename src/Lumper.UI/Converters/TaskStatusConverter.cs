using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Lumper.UI.ViewModels.Tasks;

namespace Lumper.UI.Converters;

/// <summary>
///     format TaskStatus
/// </summary>
public class TaskStatusConverter : IValueConverter
{
    public static TaskStatusConverter Instance
    {
        get;
    } = new();

    public object Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (value is not TaskStatus status)
        {
            return new BindingNotification(
                new ArgumentOutOfRangeException(nameof(value)),
                BindingErrorType.DataValidationError);
        }
        switch (status)
        {
            case TaskStatus.Waiting:
                return "|";
            case TaskStatus.Running:
                return "➠";
            case TaskStatus.Success:
                return "✓";
            case TaskStatus.Failed:
                return "⚠";
            case TaskStatus.Unknown:
            default:
                return " ";
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
