namespace Lumper.UI.Services;

using System.Runtime.Serialization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

/// <summary>
/// Singleton for storing any state we want to persist between sessions.
/// </summary>
[DataContract]
public class StateService : ReactiveObject
{
    // This is instantiated before the app window even opens, safe to `null!`
    public static StateService Instance { get; private set; } = null!;

    // Nasty public ctor needed for RxApp.SuspensionHost.CreateNewAppState
    // `() => StateService.Instance` seems to result in multiple instances
    // somehow??
    public StateService() => Instance = this;

    [DataMember, Reactive]
    public bool SaveCompressed { get; set; } = false;

    [DataMember, Reactive]
    public bool MakeBackup { get; set; } = true;

    [DataMember, Reactive]
    public bool LogShowDebug { get; set; } = false;

    [DataMember, Reactive]
    public bool LogAutoScroll { get; set; } = true;

    [DataMember, Reactive]
    public bool VtfBrowserShowCubemaps { get; set; } = false;
}
