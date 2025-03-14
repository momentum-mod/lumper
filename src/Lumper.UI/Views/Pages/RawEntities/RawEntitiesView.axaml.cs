namespace Lumper.UI.Views.Pages.RawEntities;

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using AvaloniaEdit;
using Lumper.Lib.Bsp;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Pages.RawEntities;
using ReactiveUI;

public partial class RawEntitiesView : ReactiveUserControl<RawEntitiesViewModel>
{
    public RawEntitiesView()
    {
        InitializeComponent();

        TextEditor editor = this.FindControl<TextEditor>("TextEditor")!;
        editor.Encoding = BspFile.Encoding;
        editor.ShowLineNumbers = true;
        editor.ContextMenu = new ContextMenu
        {
            ItemsSource = new List<MenuItem>
            {
                new()
                {
                    Header = "Copy",
                    InputGesture = new KeyGesture(Key.C, KeyModifiers.Control),
                    Command = ReactiveCommand.Create(editor.Copy),
                },
                new()
                {
                    Header = "Paste",
                    InputGesture = new KeyGesture(Key.V, KeyModifiers.Control),
                    Command = ReactiveCommand.Create(editor.Paste),
                },
                new()
                {
                    Header = "Cut",
                    InputGesture = new KeyGesture(Key.X, KeyModifiers.Control),
                    Command = ReactiveCommand.Create(editor.Cut),
                },
            },
        };

        this.WhenActivated(disposables =>
        {
            BspService
                .Instance.WhenAnyValue(x => x.EntityLumpViewModel)
                .Subscribe(entlump =>
                    Observable.Start(() => ViewModel!.LoadEntityLump(entlump, editor), RxApp.MainThreadScheduler)
                )
                .DisposeWith(disposables);

            Disposable
                .Create(() => Observable.Start(ViewModel!.SaveOrDiscardEntityLump, RxApp.MainThreadScheduler))
                .DisposeWith(disposables);
        });
    }
}
