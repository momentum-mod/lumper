using System;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;
using Lumper.UI.ViewModels.Bsp;

namespace Lumper.UI.Converters;

public class BmpNodeTitleConverter : IValueConverter
{
    public static BmpNodeTitleConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is BspNodeBase bspNode)
            return BuildName(bspNode);
        throw new NotSupportedException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string BuildName(BspNodeBase node)
    {
        var builder = new StringBuilder();
        builder.Append(node);
        var currentNode = node;
        while (currentNode is not null)
        {
            builder.Insert(0, $"{currentNode.NodeName}.");
            currentNode = currentNode.Parent;
        }

        return builder.ToString();
    }
}