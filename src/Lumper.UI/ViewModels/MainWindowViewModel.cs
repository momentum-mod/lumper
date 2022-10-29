using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using Lumper.Lib.BSP;
using Lumper.UI.ViewModels.Bsp;
using Lumper.UI.ViewModels.Matchers;
using ReactiveUI;

namespace Lumper.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private BspViewModel? _bspModel;
    private Dictionary<BspNodeBase, UserControl> _bspTabCache = new();
    private BspFile? _file;
    private readonly SourceList<BspNodeBase> _openTabs = new();
    private readonly ReadOnlyObservableCollection<BspNodeBase> _openTabsReadOnly;
    private readonly HashSet<BspNodeBase> _openTabsSet = new();
    private string _searchPattern = "";
    private MatcherBase _selectedMatcher = new GlobMatcherViewModel();
    private BspNodeBase? _selectedNode;
    private BspNodeBase? _selectedTab;

    public MainWindowViewModel()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
            desktop)
            throw new InvalidCastException(nameof(Application.Current.ApplicationLifetime));

        Desktop = desktop;

        this.WhenAnyValue(x => x.SearchPattern, x => x.SelectedMatcher, x => x.BspModel)
            .Throttle(TimeSpan.FromMilliseconds(400))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(Search);

        this.WhenAnyValue(x => x.BspModel)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => this.RaisePropertyChanged(nameof(LoadedPath)));

        this.WhenAnyValue(x => x.SelectedNode)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => Open(x));

        _openTabs.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _openTabsReadOnly)
            .DisposeMany()
            .Subscribe(_ => { }, RxApp.DefaultExceptionHandler.OnNext);

        RxApp.MainThreadScheduler.Schedule(OnLoad);
    }

    public IClassicDesktopStyleApplicationLifetime Desktop { get; }

    public string LoadedPath => BspModel?.FilePath ?? "";

    public BspViewModel? BspModel
    {
        get => _bspModel;
        set => this.RaiseAndSetIfChanged(ref _bspModel, value);
    }

    public BspNodeBase? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    public BspNodeBase? SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    public string SearchPattern
    {
        get => _searchPattern;
        set => this.RaiseAndSetIfChanged(ref _searchPattern, value);
    }

    public MatcherBase SelectedMatcher
    {
        get => _selectedMatcher;
        set => this.RaiseAndSetIfChanged(ref _selectedMatcher, value);
    }

    public ReadOnlyObservableCollection<BspNodeBase> OpenTabs => _openTabsReadOnly;

    private async void OnLoad()
    {
        if (Desktop.Args is not { Length: 1 })
            return;
        await LoadBsp(Desktop.Args[0]);
    }

    public void Open(BspNodeBase? bspNode)
    {
        if (bspNode is null || !bspNode.CanView)
            return;
        if (_openTabsSet.Add(bspNode))
            _openTabs.Add(bspNode);
        SelectedTab = bspNode;
    }

    public void Close(BspNodeBase? bspNode)
    {
        if (bspNode is null)
            return;
        if (_openTabsSet.Remove(bspNode))
            _openTabs.Remove(bspNode);
    }

    private async void Search((string?, MatcherBase?, BspViewModel?) args)
    {
        var (pattern, matcherBase, model) = args;
        if (matcherBase is null || pattern is null || model is null)
            return;
        //TODO: Add lock when search is slower than throttle rate
        var matcher = matcherBase.ConstructMatcher(pattern.Trim());
        await model.Filter(matcher);
    }

    public async ValueTask OpenCommand()
    {
        var dialog = ConstructOpenBspDialog();
        var result = await dialog.ShowAsync(Desktop.MainWindow);
        if (result is not { Length: 1 })
            return;
        var path = result.First();
        await LoadBsp(path);
    }

    private static OpenFileDialog ConstructOpenBspDialog()
    {
        var dialog = new OpenFileDialog();
        var bspFilter = new FileDialogFilter { Name = "Bsp file" };
        bspFilter.Extensions.Add("bsp");
        var anyFilter = new FileDialogFilter { Name = "All files" };
        anyFilter.Extensions.Add("*");
        dialog.Filters!.Add(bspFilter);
        dialog.Filters!.Add(anyFilter);
        dialog.AllowMultiple = false;
        dialog.Title = "Pick bsp file";
        return dialog;
    }

    private async ValueTask LoadBsp(string path)
    {
        if (!File.Exists(path))
            return;
        var file = new BspFile();
        file.Load(path);
        _file = file;
        BspModel = new BspViewModel(this, file);
    }
}