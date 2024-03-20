namespace Lumper.UI.Converters;
using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Lumper.UI.ViewModels.Tasks;

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
        return status switch
        {
            TaskStatus.Waiting => "|",
            TaskStatus.Running => "➠",
            TaskStatus.Success => "✓",
            TaskStatus.Failed => "⚠",
            _ => " ",
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture) => new BindingNotification(
            new ArgumentOutOfRangeException(nameof(value)),
            BindingErrorType.DataValidationError);
}
