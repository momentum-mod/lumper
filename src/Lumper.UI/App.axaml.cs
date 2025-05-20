namespace Lumper.UI;

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Lumper.UI.Services;
using Lumper.UI.ViewModels;
using Lumper.UI.Views;
using ReactiveUI;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // If we're not a desktop lifetime, we're probably an in-editor UI preview - abandon ship!
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        // Data persistence logic based on
        // https://docs.avaloniaui.net/docs/concepts/reactiveui/data-persistence#creating-the-suspension-driver
        // https://www.reactiveui.net/docs/handbook/data-persistence.html
        //
        // Avalonia page mentions viewmodel "tree", which would require hierarchical
        // serializations of viewmodels as properties of their parents.
        // Our lazy loading PageService stuff means we don't store child viewmodels directly
        // on viewmodels, so annoying to get working.
        // We don't need to persist very much anyway, so using a single "StateService"
        // singleton that stores everything we want to persist.

        // Initialize suspension helper using application lifetime
        var suspension = new AutoSuspendHelper(desktop);

        // Instantiate instance of StateService which will register itself as the static
        // StateService.Instance property. `() => StateService.Instance()` doesn't work
        // unfortunately.
        RxApp.SuspensionHost.CreateNewAppState = () => new StateService();
        RxApp.SuspensionHost.SetupDefaultSuspendResume(new JsonSuspensionDriver(AppStatePath));
        suspension.OnFrameworkInitializationCompleted();

        // Init main window
        desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
        base.OnFrameworkInitializationCompleted();
    }

    private static string AppStatePath => Path.Combine(AppContext.BaseDirectory, "appstate.json");
}
