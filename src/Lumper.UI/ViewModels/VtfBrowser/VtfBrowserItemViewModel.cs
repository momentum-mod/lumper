namespace Lumper.UI.ViewModels.VtfBrowser;
using System.Linq;
using Models.VTF;
using ReactiveUI;
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

    private Image<Rgba32>? _image;
    public Image<Rgba32>? Image
    {
        get
        {
            _image ??= vtfFile.GetImage(0, 0, 0, 0);
            return _image;
        }
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }
}
