namespace Lumper.UI.ViewModels;

using Avalonia.Controls;
using ReactiveUI;
using ViewLocator = ViewLocator;

/// <summary>
/// Base for all ViewModels
/// See https://www.reactiveui.net/docs/handbook/view-models/index.html
/// </summary>
public class ViewModel : ReactiveObject
{
    protected void RegisterView<TViewModel, TView>()
        where TViewModel : ViewModel
        where TView : Control, IViewFor<TViewModel>, new() =>
        ViewLocator.Register<TViewModel, TView>(() => new TView());
};

/// <summary>
/// Associate a ViewModel with a particular View for use by the ViewLocator
/// </summary>
public class ViewModelWithView<TViewModel, TView> : ViewModel
    where TViewModel : ViewModel
    where TView : Control, IViewFor<TViewModel>, new()
{
    protected ViewModelWithView() => ViewLocator.Register<TViewModel, TView>(() => new TView());
}
