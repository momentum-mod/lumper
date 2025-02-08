namespace Lumper.UI.Services;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Lumper.UI.ViewModels;
using Lumper.UI.ViewModels.Pages.EntityEditor;
using Lumper.UI.ViewModels.Pages.EntityReview;
using Lumper.UI.ViewModels.Pages.Jobs;
using Lumper.UI.ViewModels.Pages.PakfileExplorer;
using Lumper.UI.ViewModels.Pages.RawEntities;
using Lumper.UI.ViewModels.Pages.VtfBrowser;
using NLog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

// This would live in PageService but then Avalonia x:Static can't access it.
// https://github.com/AvaloniaUI/Avalonia/issues/13452
public enum Page
{
    EntityEditor,
    PakfileExplorer,
    VtfBrowser,
    Jobs,
    RawEntities,
    EntityReview,
}

/// <summary>
/// Service handling lazy loading and switching between different pages.
///
/// This may seem like overkill, but some pages (like texture browser) are prohibitively
/// expensive, so we need to only load when used, including the case where a user loads
/// one BSP, opens the texture browser, switches pages, then opens another BSP.
/// </summary>
public sealed class PageService : ReactiveObject
{
    public static PageService Instance { get; } = new();

    // Collection of all available pages.
    // For performance, ViewModels are only constructed when the pages are accessed.
    // When a new BSP is loaded, any inactive ephemeral viewmodels are discarded.
    private readonly Dictionary<Page, ILazyPage<ViewModel>> _pageVms = new()
        new()
        {
            { Page.EntityEditor, new LazyPage<EntityEditorViewModel>(true) },
            { Page.PakfileExplorer, new LazyPage<PakfileExplorerViewModel>(true) },
            { Page.VtfBrowser, new LazyPage<VtfBrowserViewModel>(true) },
            { Page.Jobs, new LazyPage<JobsViewModel>(false) },
            { Page.RawEntities, new LazyPage<RawEntitiesViewModel>(true) },
            { Page.EntityReview, new LazyPage<EntityReviewViewModel>(true) },
        };

    [Reactive]
    public ViewModel? ActivePageVm { get; set; }

    [Reactive]
    public Page? ActivePage { get; private set; }

    [Reactive]
    public Page? PreviousPage { get; private set; }

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private PageService() =>
        BspService
            .Instance.WhenAnyValue(x => x.BspFile)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                foreach ((Page page, ILazyPage<ViewModel> pageVm) in _pageVms)
                {
                    // When active BSP file changes (incl. closing), reset any page that is
                    //  - loaded
                    //  - ephemeral, i.e. should be reset on BSP change
                    //  - not the current page
                    // If we were to reset the *current* page (and then navigate straight back to it), we'd
                    // have two observable streams that affect the state of that page:
                    //  (a) the active BSP, and
                    //  (b) the lump viewmodel that that page displays (since the page constructors typically contain a
                    //      WhenAny which notifies immediately).
                    // That makes the logic far too complicated - a single stream should be responsible
                    // for the state of that page. So just leave the active page as-is and it can react to its lumps changing.
                    if (pageVm.IsLoaded() && pageVm.Ephemeral && ActivePage != page)
                        pageVm.Reset();
                }
            });

    public void ViewPage(Page page)
    {
        if (page == ActivePage)
            return;

        if (!_pageVms.TryGetValue(page, out ILazyPage<ViewModel>? pageVm))
            throw new ArgumentException($"Bad page name {page}");

        PreviousPage = ActivePage;
        ActivePage = page;
        try
        {
            ActivePageVm = pageVm.Get();
        }
        catch (Exception ex)
        {
            // Hopefully catches anything thrown up when loading page or shared VMs
            _logger.Error(ex, "Failed to load page");
            ActivePage = null;
            ActivePageVm = null;
        }
    }

    // This wrapper interface + class is required because we need a covariant type for Get(),
    // so that e.g. LazyPage<VtfBrowserViewModel> is assignable to LazyPage<ViewModel>.
    private interface ILazyPage<out T>
    {
        public bool Ephemeral { get; }

        public T Get();
        public bool IsLoaded();
        public void Reset();
    }

    private sealed class LazyPage<T>(bool ephemeral) : ILazyPage<T>
        where T : ViewModel, new()
    {
        public bool Ephemeral { get; } = ephemeral;
        private Lazy<T> _lazyWrapper = new();

        public T Get() => _lazyWrapper.Value;

        public bool IsLoaded() => _lazyWrapper.IsValueCreated;

        public void Reset() => _lazyWrapper = new Lazy<T>();
    }
}
