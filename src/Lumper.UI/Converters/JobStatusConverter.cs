namespace Lumper.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Lumper.UI.ViewModels.Pages.Jobs;
using Material.Icons;

public class JobStatusConverter : IValueConverter
{
    public static JobStatusConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not JobStatus status)
        {
            return new BindingNotification(
                new ArgumentOutOfRangeException(nameof(value)),
                BindingErrorType.DataValidationError
            );
        }

        return status switch
        {
            JobStatus.Waiting => MaterialIconKind.TimerSandEmpty,
            JobStatus.Running => MaterialIconKind.TimerSand,
            JobStatus.Success => MaterialIconKind.Check,
            JobStatus.Failed => MaterialIconKind.Warning,
            _ => throw new NotImplementedException(), // No fucking clue why Roslyn insists we have this
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(
            new ArgumentOutOfRangeException(nameof(value)),
            BindingErrorType.DataValidationError
        );
    }
}
