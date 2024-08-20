namespace Lumper.UI.Views.Shared;

using Avalonia.ReactiveUI;
using Lumper.UI.ViewModels.Shared.Pakfile;

public partial class VtfImageWindow : ReactiveWindow<PakfileEntryVtfViewModel>
{
    public VtfImageWindow()
    {
        InitializeComponent();

        Closing += (_, _) => ViewModel!.HasSeparateWindow = false;
    }
}
