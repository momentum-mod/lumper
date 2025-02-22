namespace Lumper.UI.ViewModels.BspInfo;

using Lumper.UI.Services;
using Lumper.UI.Views.BspInfo;
using ReactiveUI.Fody.Helpers;

public sealed class BspInfoViewModel : ViewModelWithView<BspInfoViewModel, BspInfoView>
{
    public static BspService BspService => BspService.Instance;

    [Reactive]
    public string Sausage { get; set; } = """Suasage""";
}
