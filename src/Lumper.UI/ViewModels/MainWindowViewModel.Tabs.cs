using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using Lumper.UI.ViewModels.Bsp;
using ReactiveUI;

namespace Lumper.UI.ViewModels;

// MainWindowViewModel support for Tabs
public partial class MainWindowViewModel
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
        this.WhenAnyValue(x => x.BspModel)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                _openTabs.Clear();
                _openTabsSet.Clear();
                _selectedTab = null;
            });

        this.WhenAnyValue(x => x.SelectedNode)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => Open(x));

        _openTabs.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _openTabsReadOnly)
            .DisposeMany()
            .Subscribe(_ => { }, RxApp.DefaultExceptionHandler.OnNext);
    }

    private void OnLoad()
    {
        if (Desktop.Args is not { Length: 1 })
            return;
        LoadBsp(Desktop.Args[0]);
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
