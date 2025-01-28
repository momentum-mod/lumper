namespace Lumper.UI;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Lumper.UI.Services;
using Lumper.UI.ViewModels;
using Lumper.UI.Views;
using ReactiveUI;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Data persistence logic based on
        // https://docs.avaloniaui.net/docs/concepts/reactiveui/data-persistence#creating-the-suspension-driver
        // https://www.reactiveui.net/docs/handbook/data-persistence.html
        //
        // Avalonia page mentions viewmodel "tree", which would require hierarchical
        // [DataContract]/[DataMember]s of viewmodels as properties of their parents,
        // something like that. Seems very awkward to make work with lazy page loading
        // stuff.
        // We don't need to persist very much anyway, so just using a single "StateService"
        // singleton that stores everything we want to persist.

        // Initialize suspension helper using application lifetime
        var suspension = new AutoSuspendHelper(Program.Desktop);

        // Instantiate instance of StateService which will register itself as the static
        // StateService.Instance property. `() => StateService.Instance()` doesn't work
        // unfortunately.
        RxApp.SuspensionHost.CreateNewAppState = () => new StateService();
        RxApp.SuspensionHost.SetupDefaultSuspendResume(new JsonSuspensionDriver("appstate.json"));
        suspension.OnFrameworkInitializationCompleted();

        // Init main window
        Program.Desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
        base.OnFrameworkInitializationCompleted();
    }
}
