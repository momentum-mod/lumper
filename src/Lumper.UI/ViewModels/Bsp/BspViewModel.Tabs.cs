using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp;

// BspViewModel support for Tabs
public partial class BspViewModel
{
    private readonly SourceList<BspNodeBase> _openTabs = new();
    private readonly HashSet<BspNodeBase> _openTabsSet = new();

    private /*readonly*/
        ReadOnlyObservableCollection<BspNodeBase> _openTabsReadOnly = null!;

    private BspNodeBase? _selectedTab;

    public BspNodeBase? SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    public ReadOnlyObservableCollection<BspNodeBase> OpenTabs =>
        _openTabsReadOnly;

    private void TabsInit()
    {
        _openTabs.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _openTabsReadOnly)
            .DisposeMany()
            .Subscribe(_ => { }, RxApp.DefaultExceptionHandler.OnNext);
    }

    public void Open(BspNodeBase? bspNode)
    {
        if (bspNode?.ViewNode is not { } viewNode)
            return;
        if (_openTabsSet.Add(viewNode))
            _openTabs.Add(viewNode);
        SelectedTab = viewNode;
    }

    public void Close(BspNodeBase? bspNode)
    {
        if (bspNode is null)
            return;
        if (_openTabsSet.Remove(bspNode))
            _openTabs.Remove(bspNode);
    }
}
