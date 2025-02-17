namespace Lumper.UI.Services;

using System;
using DynamicData.Binding;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

/// <summary>
/// Singleton for storing any state we want to persist between sessions.
/// </summary>
[JsonObject(MemberSerialization.OptOut)]
public class StateService : ReactiveObject
{
    // This is instantiated before the app window even opens, safe to `null!`
    public static StateService Instance { get; private set; } = null!;

    // Nasty public ctor needed for RxApp.SuspensionHost.CreateNewAppState
    // `() => StateService.Instance` seems to result in multiple instances
    // somehow??
    public StateService() => Instance = this;

    [Reactive]
    public long LastUpdateCheck { get; set; } = 0;

    [Reactive]
    public bool SaveCompressed { get; set; } = false;

    [Reactive]
    public bool MakeBackup { get; set; } = true;

    [Reactive]
    public ushort GameSyncPort { get; set; }

    [Reactive]
    public bool RenameCubemaps { get; set; } = true;

    [Reactive]
    public bool LogShowDebug { get; set; } = false;

    [Reactive]
    public bool LogAutoScroll { get; set; } = true;

    [Reactive]
    public bool VtfBrowserShowCubemaps { get; set; } = false;

    [Reactive]
    // Using a power of 2 doesn't have a significant improvement visually and 128/256 sizes feel too small/large
    public double VtfBrowserDimensions { get; set; } = 192;

    public ObservableCollectionExtended<string> RecentFiles { get; set; } = [];

    public void UpdateRecentFiles(string bspPath, bool opened)
    {
        using IDisposable suspend = RecentFiles.SuspendNotifications();

        // If we just opened a new BSP, remove from recent list if in there
        if (opened)
        {
            RecentFiles.Remove(bspPath);
            return;
        }

        // Otherwise add closed BSP to the top of the list, removing any duplicates,
        // and capping the list at 5 entries.
        RecentFiles.Remove(bspPath);
        RecentFiles.Add(bspPath);

        const int maxRecent = 5;
        if (RecentFiles.Count > maxRecent)
            RecentFiles.RemoveAt(0);
    }
}
