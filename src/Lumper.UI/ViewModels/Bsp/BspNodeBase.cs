using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Lumper.UI.Models;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp;

public abstract class BspNodeBase : ViewModelBase
{
    private bool _canView;
    private ReadOnlyObservableCollection<BspNodeBase>? _filteredNodes;
    private bool _isVisible;
    private ReadOnlyObservableCollection<BspNodeBase>? _nodes;
    private BspNodeBase? _selectedNode;

    public BspNodeBase()
    {
        _isVisible = true;
    }

    public BspNodeBase(BspNodeBase parent)
    {
        Parent = parent;
        _isVisible = parent._isVisible;
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public bool CanView
    {
        get => _canView;
        set => this.RaiseAndSetIfChanged(ref _canView, value);
    }

    public bool IsLeaf => _nodes is not { Count: > 0 };
    public BspNodeBase? Parent { get; }

    public ReadOnlyObservableCollection<BspNodeBase>? Nodes => _nodes;
    public ReadOnlyObservableCollection<BspNodeBase>? FilteredNodes => _filteredNodes;

    public BspNodeBase? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    public abstract string NodeName { get; }
    public abstract string NodeIcon { get; }

    protected virtual async Task<bool> Match(Matcher matcher, CancellationToken? cancellationToken)
    {
        return await matcher.Match(NodeName);
    }

    public async Task Reset()
    {
        IsVisible = true;
        if (_nodes is not null)
            foreach (var node in _nodes)
                await node.Reset();
    }

    public async Task<bool> Filter(Matcher matcher, CancellationToken? cancellationToken = null)
    {
        var anyChildVisible = false;
        if (_nodes is not null)
        {
            //TODO: Add visibility cache for restoration of changed state
            if (cancellationToken is { IsCancellationRequested: true })
                return _isVisible;
            foreach (var node in _nodes)
                anyChildVisible |= await node.Filter(matcher);
        }

        if (cancellationToken is { IsCancellationRequested: true })
            return _isVisible;
        var visible = anyChildVisible || await Match(matcher, cancellationToken);
        IsVisible = visible;
        return visible;
    }

    private Func<BspNodeBase, bool> MakeFilter(bool x)
    {
        return demoClass => demoClass.IsVisible;
    }

    protected void InitializeNodeChildrenObserver<T>(ISourceList<T> list) where T : BspNodeBase
    {
        if (_nodes is not null || _filteredNodes is not null)
            throw new Exception("Nodes cannot be bound to multiple lists");

        list.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(t => (BspNodeBase)t)
            .Bind(out _nodes)
            .DisposeMany()
            .Subscribe(_ => { }, RxApp.DefaultExceptionHandler.OnNext);

        list.Connect()
            .AutoRefreshOnObservable(x => x.WhenValueChanged(y => y.IsVisible))
            .Filter(x => x._isVisible)
            .Transform(t => (BspNodeBase)t)
            .Bind(out _filteredNodes)
            .DisposeMany()
            .Subscribe(_ => { }, RxApp.DefaultExceptionHandler.OnNext);
    }
}