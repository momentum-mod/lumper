namespace Lumper.UI.Views.Pages.Jobs;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Lumper.UI.ViewModels.Pages.Jobs;
using ReactiveUI;

public partial class StripperTextJobView : ReactiveUserControl<StripperTextJobViewModel>
{
    public StripperTextJobView()
    {
        InitializeComponent();

        this.WhenActivated(_ =>
        {
            this.Bind(
                ViewModel,
                viewModel => viewModel.Config,
                view => view.TextEditor.Text,
                // Probably quite inefficent getting this entire string every change, perf is acceptable though.
                Observable
                    .FromEventPattern(
                        handler => TextEditor.TextChanged += handler,
                        handler => TextEditor.TextChanged -= handler
                    )
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .ObserveOn(RxApp.MainThreadScheduler)
            );

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
