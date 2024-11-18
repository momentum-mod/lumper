namespace Lumper.UI.Views;

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Lib.BSP.IO;
using ReactiveUI;
using ViewModels;

public partial class IoProgressWindow : ReactiveWindow<ViewModel>
{
    public IoProgressWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (Handler is null)
                return;

            TitleDisplay.Text = Title;

            // Throws if not on UI thread. Need to use events since this IoProgress stuff is part of Lumper.Lib.
            Observable
                .FromEventPattern<IoProgressEventArgs>(h => Handler.Progress += h, h => Handler.Progress -= h)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x.EventArgs)
                .Subscribe(args =>
                {
                    ProgressBar.Value = args.ProgressPercent;
                    if (args.Message is not null)
                        Status.Text = args.Message;
                })
                .DisposeWith(disposables);
        });
    }

    public required IoHandler Handler { get; init; }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Handler.CancellationTokenSource.Cancel();
        Close();
    }
}
