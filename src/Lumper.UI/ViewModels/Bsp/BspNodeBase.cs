using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Lumper.UI.Models;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp;

/// <summary>
///     ViewModel base for <see cref="Lumper.Lib.BSP.Lumps.Lump" /> TreeNode representation
/// </summary>
public abstract class BspNodeBase : ViewModelBase
{
    private ReadOnlyObservableCollection<BspNodeBase>? _filteredNodes;
    private bool _isVisible;
    private bool _isExpanded;
    private ReadOnlyObservableCollection<BspNodeBase>? _nodes;

    public BspNodeBase(BspViewModel bspView)
    {
        Parent = null;
        _isVisible = true;
        _isExpanded = false;
        BspView = bspView;
    }

    public BspNodeBase(BspNodeBase parent)
    {
        Parent = parent;
        _isVisible = parent._isVisible;
        _isExpanded = parent._isExpanded;
        BspView = parent.BspView;
    }

    public bool IsModifiedRecursive => IsModified
                                       || _nodes is not null && _nodes.Any(n =>
                                           n.IsModifiedRecursive);

    public virtual bool IsModified => false;

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public virtual BspNodeBase? ViewNode => null;

    public bool IsLeaf => _nodes is not { Count: > 0 };

    public BspNodeBase? Parent
    {
        get;
    }

    public BspViewModel BspView
    {
        get;
    }

    public ReadOnlyObservableCollection<BspNodeBase>? Nodes => _nodes;

    public ReadOnlyObservableCollection<BspNodeBase>? FilteredNodes =>
        _filteredNodes;


    public abstract string NodeName
    {
        get;
    }

    public virtual string NodeIcon => "/Assets/Lumper.png";

    public virtual void Update()
    {
        if (_nodes is { Count: > 0 })
            foreach (var node in _nodes)
                node.Update();

        this.RaisePropertyChanged(nameof(IsModified));
        this.RaisePropertyChanged(nameof(IsModifiedRecursive));
    }

    protected virtual async ValueTask<bool> Match(Matcher matcher,
        CancellationToken? cancellationToken)
    {
        return await matcher.Match(NodeName);
    }

    public virtual void Open()
    { }

    public void Close()
    {
        BspView.Close(this);
    }

    //hack used for the hotkey on invisible button
    //hotkey always calls the first tab
    //adding and removing the hotkey in code behind didn't work
    //hotkey in mainwindow menu didn't work either
    //so this is my ugly workaround
    public void CloseSelected()
    {
        BspView.Close(BspView.SelectedTab);
    }

    public async ValueTask Reset()
    {
        IsVisible = true;
        if (_nodes is not null)
            foreach (var node in _nodes)
                await node.Reset();
    }

    public async ValueTask<bool> Filter(Matcher matcher,
        CancellationToken? cancellationToken = null)
    {
        bool anyChildVisible = false;
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
        bool visible =
            anyChildVisible || await Match(matcher, cancellationToken);
        IsExpanded = !matcher.IsEmpty && anyChildVisible;
        IsVisible = visible;
        return visible;
    }

    protected void InitializeNodeChildrenObserver<T>(ISourceList<T> list)
        where T : BspNodeBase
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

        // This is a probably hacky way of waiting for loading to complete before registering the
        // IsModified change subscriptions. Using a Subject is probably wrong but I don't know
        // Reactive C# stuff well.
        BspView.Loading
            .Where(x => !x)
            .FirstAsync()
            .Subscribe(
                _ =>
                {
                    list.Connect()
                        .AutoRefreshOnObservable(x => x.WhenValueChanged(y => y.IsModified))
                        .Subscribe(_ => { this.RaisePropertyChanged(nameof(IsModified)); });

                    // TODO: Why are there two versions of this??
                    list.Connect()
                        .AutoRefreshOnObservable(x =>
                            x.WhenValueChanged(y => y.IsModifiedRecursive))
                        .Subscribe(_ =>
                        {
                            this.RaisePropertyChanged(nameof(IsModifiedRecursive));
                        });
                });
    }

    public TRet Modify<TRet>(
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string? propertyName = null)
    {
        var result =
            this.RaiseAndSetIfChanged(ref backingField, newValue, propertyName);
        this.RaisePropertyChanged(nameof(IsModified));
        this.RaisePropertyChanged(nameof(IsModifiedRecursive));
        if (ViewNode is not null && ViewNode != this)
            ViewNode.RaisePropertyChanged(nameof(IsModified));

        return result;
    }

    public void ExpandTree()
    {
        ExpandTree(_ => true);
    }
    public void ExpandTree(Func<BspNodeBase, bool> fun)
    {
        IsExpanded = fun(this);
        if (Nodes is not null)
        {
            foreach (var node in Nodes)
            {
                node.ExpandTree(fun);
            }
        }
    }
}
