namespace Lumper.UI.Views.Shared.Pakfile;

using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Lumper.Lib.Bsp;
using Lumper.UI.ViewModels.Shared.Pakfile;
using ReactiveUI;

public partial class PakfileEntryTextView : ReactiveUserControl<PakfileEntryTextViewModel>
{
    public PakfileEntryTextView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.Bind(
                    ViewModel,
                    viewModel => viewModel.Content,
                    view => view.TextEditor.Text,
                    Observable.FromEventPattern(
                        handler => TextEditor.TextChanged += handler,
                        handler => TextEditor.TextChanged -= handler
                    )
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
