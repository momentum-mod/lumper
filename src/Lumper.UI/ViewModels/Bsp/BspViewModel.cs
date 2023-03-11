using ReactiveUI;
using Lumper.Lib.BSP;
using Lumper.UI.ViewModels.Bsp.Lumps;

namespace Lumper.UI.ViewModels.Bsp;

/// <summary>
///     View model for <see cref="Lumper.Lib.BSP.BspFile" />
/// </summary>
public partial class BspViewModel : ViewModelBase
{
    private string? _filePath;

    public BspViewModel(BspFile bspFile)
    {
        BspFile = bspFile;
        BspNode = new BspNodeViewModel(this);
        SearchInit();
        TabsInit();
    }

    public BspFile BspFile
    {
        get;
    }

    public BspNodeBase BspNode
    {
        get;
    }
    private BspNodeBase? _selectedNode;
    public BspNodeBase? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    public string? FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    public void Update()
    {
        BspNode.Update();
    }
}
