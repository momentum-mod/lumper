using System;
using System.Globalization;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Lumper.UI.ViewModels.Bsp;

namespace Lumper.UI.Converters;

/// <summary>
///     Generates full name for bsp node.
/// </summary>
public class BmpNodeTitleConverter : IValueConverter
{
    public static BmpNodeTitleConverter Instance
    {
        get;
    } = new();

    public object Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (value is BspNodeBase bspNode)
            return BuildName(bspNode);
        return new BindingNotification(
            new ArgumentOutOfRangeException(nameof(value)),
            BindingErrorType.DataValidationError);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        return new BindingNotification(
            new ArgumentOutOfRangeException(nameof(value)),
            BindingErrorType.DataValidationError);
    }

    private static string BuildName(BspNodeBase node)
    {
        var builder = new StringBuilder();
        builder.Append(node.NodeName);
        var currentNode = node;
        while (currentNode is not null)
        {
            builder.Insert(0, $"{currentNode.NodeName}.");
            currentNode = currentNode.Parent;
        }

        return builder.ToString();
    }
}
