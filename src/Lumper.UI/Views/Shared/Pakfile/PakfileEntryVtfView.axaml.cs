namespace Lumper.UI.Views.Shared.Pakfile;

using Avalonia.Input;
using Avalonia.ReactiveUI;
using Lumper.UI.ViewModels.Shared.Pakfile;

public partial class PakfileEntryVtfView : ReactiveUserControl<PakfileEntryVtfViewModel>
{
    public PakfileEntryVtfView()
    {
        InitializeComponent();
    }

    private void Image_OnPointerPressed(object? _, PointerPressedEventArgs __)
    {
        ViewModel!.OpenVtfImageWindow();
    }
}
