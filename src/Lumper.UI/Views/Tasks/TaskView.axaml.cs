namespace Lumper.UI.Views.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

public partial class TaskView : UserControl
{
    public TaskView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
