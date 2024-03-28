namespace Lumper.UI.ViewModels.VtfBrowser;
using System.Linq;
using Models.VTF;
using ReactiveUI.Fody.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class VtfBrowserItemViewModel(string key, VtfFile vtfFile) : ViewModelBase
{
    [Reactive]
    public bool IsVisible { get; set; } = true;

    [Reactive]
    public string Name { get; set; } = key.Split('/').Last();

    [Reactive]
    public string Path { get; set; } = key;

    [Reactive]
    public Image<Rgba32>? Image { get; set; } = vtfFile.GetImage(0, 0, 0, 0);
}
