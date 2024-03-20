namespace Lumper.UI.ViewModels.Bsp;
using System.IO;
using Lumper.Lib.BSP;
using Lumper.UI.ViewModels.Bsp.Lumps;
using ReactiveUI;

/// <summary>
///     View model for <see cref="Lib.BSP.BspFile" />
/// </summary>
public partial class BspViewModel : ViewModelBase, System.IDisposable
{
    private string? _filePath;
    private string? _fileName;

    public BspViewModel(BspFile bspFile)
    {
        BspFile = bspFile;

        FilePath = BspFile.FilePath;
        FileName = Path.GetFileNameWithoutExtension(BspFile.FilePath);
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

    public string? FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }


    public void Update() => BspNode.Update();

    public void Dispose() => throw new System.NotImplementedException();
}
