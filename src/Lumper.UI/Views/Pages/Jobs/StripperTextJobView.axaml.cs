namespace Lumper.UI.Views.Pages.Jobs;

using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Lumper.Lib.Bsp;
using Lumper.UI.ViewModels.Pages.Jobs;
using ReactiveUI;

public partial class StripperTextJobView : ReactiveUserControl<StripperTextJobViewModel>
{
    public StripperTextJobView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.Bind(
                    ViewModel,
                    viewModel => viewModel.Config,
                    view => view.TextEditor.Text,
                    Observable
                        .FromEventPattern(
                            handler => TextEditor.TextChanged += handler,
                            handler => TextEditor.TextChanged -= handler
                        )
                        .ObserveOn(RxApp.MainThreadScheduler)
                )
                .DisposeWith(disposables);

            TextEditor.Encoding = BspFile.Encoding;
            TextEditor.ContextMenu = new ContextMenu
            {
                ItemsSource = new List<MenuItem>
                {
                    new()
                    {
                        Header = "Copy",
                        InputGesture = new KeyGesture(Key.C, KeyModifiers.Control),
                        Command = ReactiveCommand.Create(() => TextEditor.Copy()),
                    },
                    new()
                    {
                        Header = "Paste",
                        InputGesture = new KeyGesture(Key.V, KeyModifiers.Control),
                        Command = ReactiveCommand.Create(() => TextEditor.Paste()),
                    },
                    new()
                    {
                        Header = "Cut",
                        InputGesture = new KeyGesture(Key.X, KeyModifiers.Control),
                        Command = ReactiveCommand.Create(() => TextEditor.Cut()),
                    },
                },
            };
        });
    }
}
