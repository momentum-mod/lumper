namespace Lumper.UI.Views.BspInfo;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Converters;
using Lumper.UI.ViewModels.BspInfo;
using ReactiveUI;
using Services;

public partial class BspInfoView : ReactiveWindow<BspInfoViewModel>
{
    public BspInfoView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            BspService
                .Instance.WhenAnyValue(
                    x => x.PakfileLumpViewModel,
                    pakfile => (InMemory: pakfile?.InMemorySize ?? 0, Uncompressed: pakfile?.UncompressedSize ?? 0)
                )
                .Select(sizes =>
                {
                    string str = $"{FileSizeConverter.FormattedFileSize(sizes.InMemory)}";
                    return sizes.InMemory != sizes.Uncompressed
                        ? str + $" ({FileSizeConverter.FormattedFileSize(sizes.Uncompressed)} uncompressed)"
                        : str;
                })
                .BindTo(this, v => v.PakfileSize.Text)
                .DisposeWith(disposables);

            // TODO: asset checking is bit annoying, just have observable that handles pakfile reads and hashing
            // as window is open, dispose with disposals if window is closed, if reopened, computing will be able
            // to restart where it left off
        });
    }
}
