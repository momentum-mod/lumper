namespace Lumper.UI;

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Lumper.UI.ViewModels;
using ReactiveUI;

/// <summary>
/// Reflection-free ViewLocator implementation. ViewLocator using reflection breaks when using trimming.
/// Based on https://github.com/AvaloniaUI/Avalonia/discussions/14511#discussioncomment-8379078
/// </summary>
public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> Registration = [];

    public static void Register<TViewModel, TView>(Func<TView> factory)
        where TViewModel : ViewModel
        where TView : Control, IViewFor<TViewModel> => Registration.TryAdd(typeof(TViewModel), factory);

    public Control Build(object? param)
    {
        if (param is null)
            return new TextBlock { Text = "No view provided" };

        Type type = param.GetType();
        return Registration.TryGetValue(type, out Func<Control>? factory)
            ? factory()
            : new TextBlock { Text = "Not Found: " + type };
    }

    public bool Match(object? data) => data is ViewModel;
}
