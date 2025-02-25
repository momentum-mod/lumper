namespace Lumper.UI.ViewModels.BspInfo;

using Lumper.UI.Services;
using Lumper.UI.Views.BspInfo;

public sealed class BspInfoViewModel : ViewModelWithView<BspInfoViewModel, BspInfoView>
{
    public static BspService BspService => BspService.Instance;
}
