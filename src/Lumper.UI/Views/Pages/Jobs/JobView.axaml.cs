namespace Lumper.UI.Views.Pages.Jobs;

using Avalonia;
using Avalonia.Controls;

public partial class JobView : UserControl
{
    public static readonly StyledProperty<object> JobDescriptionProperty = AvaloniaProperty.Register<JobView, object>(
        nameof(JobDescription)
    );

    public static readonly StyledProperty<object> MainContentProperty = AvaloniaProperty.Register<JobView, object>(
        nameof(MainContent)
    );

    public object JobDescription
    {
        get => GetValue(JobDescriptionProperty);
        set => SetValue(JobDescriptionProperty, value);
    }

    public object MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    public JobView() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == JobDescriptionProperty)
        {
            if (this.GetControl<ContentControl>("PART_JobDescription") is { } description)
                description.Content = change.NewValue;
        }
        else if (change.Property == MainContentProperty)
        {
            if (this.GetControl<ContentControl>("PART_MainContent") is { } content)
                content.Content = change.NewValue;
        }
    }
}
