using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Lumper.UI.Models;
using Lumper.Lib.VTF;
using System.Linq;

namespace Lumper.UI.ViewModels.VtfBrowser;

public class VtfBrowserItemViewModel : ViewModelBase
{
    public VtfBrowserItemViewModel(string key, VtfFileData vtfFileData)
    {
        Path = key;
        Name = key.Split('/').Last();
        _vtfFileData = vtfFileData;
    }

    private readonly VtfFileData _vtfFileData;

    private bool _isVisible = true;

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }


    private string _name = "";

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _path = "";

    public string Path
    {
        get => _path;
        set => this.RaiseAndSetIfChanged(ref _path, value);
    }

    private Image<Rgba32>? _image = null;

    public Image<Rgba32>? Image
    {
        get
        {
            if (_image is null)
            {
                _image = _vtfFileData.GetImage(0, 0, 0, 0);
            }
            return _image;
        }
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }
}
