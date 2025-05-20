namespace Lumper.UI.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

public partial class ClearableTextBox : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<ClearableTextBox, string>(
        nameof(Text),
        defaultValue: string.Empty,
        defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
    );

    public static readonly StyledProperty<string> WatermarkProperty = AvaloniaProperty.Register<
        ClearableTextBox,
        string
    >(nameof(Watermark));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public ClearableTextBox()
    {
        InitializeComponent();
    }

    private void ClearButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Text = string.Empty;
    }
}
