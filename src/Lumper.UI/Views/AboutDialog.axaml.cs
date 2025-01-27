namespace Lumper.UI.Views;

using System.Reflection;
using Avalonia.ReactiveUI;
using Services;
using ViewModels;

public partial class AboutWindow : ReactiveWindow<ViewModel>
{
    public AboutWindow()
    {
        InitializeComponent();

        UpdaterService.SemVer version = UpdaterService.GetAssemblyVersion();

        LumperVersion.Text = $"Lumper v{version}";
        DotnetVersion.Text = $".NET {System.Environment.Version}";
        AvaloniaVersion.Text =
            $"Avalonia {Assembly.Load("Avalonia").GetName().Version?.ToString() ?? "Unknown Version"}";

        CloseButton.Click += (_, _) => Close();
    }
}
