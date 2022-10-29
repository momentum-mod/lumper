using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Lumper.Lib.BSP;
using Lumper.UI.ViewModels.Bsp;
using Lumper.UI.ViewModels.Matchers;
using ReactiveUI;

namespace Lumper.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private BspViewModel? _bspModel;
    private BspFile? _file;
    private string _searchPattern = "";

    private MatcherBase _selectedMatcher = new GlobMatcherViewModel();

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

        RxApp.MainThreadScheduler.Schedule(OnLoad);
    }

    public IClassicDesktopStyleApplicationLifetime Desktop { get; }

    public BspViewModel? BspModel
    {
        get => _bspModel;
        set => this.RaiseAndSetIfChanged(ref _bspModel, value);
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

    private async void OnLoad()
    {
        if (Desktop.Args is not { Length: 1 })
            return;
        await LoadBsp(Desktop.Args[0]);
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

    public async Task OpenCommand()
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
        bspFilter.Extensions.Add("*");
        dialog.Filters!.Add(bspFilter);
        dialog.Filters!.Add(anyFilter);
        dialog.AllowMultiple = false;
        dialog.Title = "Pick bsp file";
        return dialog;
    }

    private async Task LoadBsp(string path)
    {
        if (!File.Exists(path))
            return;
        var file = new BspFile();
        file.Load(path);
        _file = file;
        BspModel = new BspViewModel(file);
    }
}