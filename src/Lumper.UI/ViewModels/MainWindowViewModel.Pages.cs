namespace Lumper.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using EntityEditor;
using Lumper.UI.ViewModels.VtfBrowser;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;
using Tasks;

public partial class MainWindowViewModel
{
    // Collection of all available pages.
    // For performance, ViewModels are only constructed when the pages are accessed.
    // When a new BSP is loaded, any ephemeral viewmodels are discarded and recreated
    private static readonly Dictionary<string, IPage<ViewModelBase>> Pages = new()
    {
        { "EntityEditor", new EphemeralLazyPage<EntityEditorViewModel>() },
        { "VtfBrowser", new EphemeralLazyPage<VtfBrowserViewModel>() },
        { "Tasks", new LazyPage<TasksViewModel>() }
    };

    [Reactive]
    public ViewModelBase? ActivePage { get; set; }
    private string? _lastPage; // Using string for these instead of an enum for easier binding
    private const string DefaultPage = "EntityEditor";

    private void PagesInit() =>
        ActiveBspService.Instance.FileChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(file =>
            {
                _logger.Debug($"wowee! {file is null}");
                if (file is not null)
                    LoadDefaultPage();
                else
                    ActivePage = null;
            });

    public void ViewPage(string pageName)
    {
        if (!ActiveBspService.Instance.HasLoadedBsp)
            return;

        if (!Pages.TryGetValue(pageName, out IPage<ViewModelBase>? page))
            throw new ArgumentException($"Bad page name {pageName}");

        ActivePage = page.Get();
        _lastPage = pageName;
    }

    private void LoadDefaultPage()
    {
        if (!ActiveBspService.Instance.HasLoadedBsp)
            return;

        ViewPage(_lastPage ?? DefaultPage);
    }

    // This interface is required because we need a covariant type for Get(),
    // so that e.g. LazyPage<VtfBrowserViewModel> is assignable to LazyPage<ViewModelBase>.
    private interface IPage<out T>
    {
        public T Get();
    }

    /// A class that lazily stores a page's viewmodel.
    /// The viewmodel is initialized only when it is first accessed.
    private class LazyPage<T> : IPage<T> where T : ViewModelBase, new()
    {
        private Lazy<T> _lazyWrapper = new();

        public T Get() => _lazyWrapper.Value;
        protected void Reset() => _lazyWrapper = new Lazy<T>();
    }

    /// A class that lazily stores a page's viewmodel, and resets the viewmodel whenever the saved BSP is reset.
    /// The viewmodel is initialized only when it is first accessed.
    private class EphemeralLazyPage<T> : LazyPage<T> where T : ViewModelBase, new()
    {
        public EphemeralLazyPage() => ActiveBspService.Instance
            .BspUnloaded
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => Reset());
    }
}
