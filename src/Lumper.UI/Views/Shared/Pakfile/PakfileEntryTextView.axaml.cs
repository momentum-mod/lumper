namespace Lumper.UI.Views.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Lumper.UI.ViewModels.Shared.Pakfile;
using ReactiveUI;

public partial class PakfileEntryTextView : ReactiveUserControl<PakfileEntryTextViewModel>
{
    public PakfileEntryTextView()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            this.Bind(
                ViewModel,
                viewModel => viewModel.Content,
                view => view.TextEditor.Text,
                Observable.FromEventPattern<RoutedEventArgs>(
                    handler => TextEditor.LostFocus += handler,
                    handler => TextEditor.LostFocus -= handler
                )
            );

            EventHandler handler = null!;
            handler = (_, _) =>
            {
                ViewModel!.MarkAsModified();
                TextEditor.TextChanged -= handler;
            };
            TextEditor.TextChanged += handler;

            TextEditor.Encoding = Encoding.ASCII;
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
