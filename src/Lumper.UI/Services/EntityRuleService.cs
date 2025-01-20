namespace Lumper.UI.Services;

using System.Reactive.Linq;
using System.Threading.Tasks;
using Lumper.Lib.Util;
using Lumper.UI.ViewModels.Shared.Entity;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using EntityRules = System.Collections.Generic.Dictionary<string, Lumper.Lib.Util.EntityRule>;

/// <summary>
/// Singleton service handling the currently loaded entity rule set.
/// Entity rules are static once loaded, but file can be changed at runtime.
/// </summary>
public sealed class EntityRuleService : ReactiveObject
{
    /// <summary>
    /// The singleton instance to the service
    /// </summary>
    public static EntityRuleService Instance { get; } = new();

    [Reactive]
    public EntityRules? Rules { get; private set; }

    public async Task LoadRules(string? path)
    {
        Rules = await Observable.Start(() => EntityRule.LoadRules(path), RxApp.TaskpoolScheduler);

        if (BspService.Instance.EntityLumpViewModel is null)
            return;

        // We'd get better encapsulation if EntityLumpViewmodels listened to Rules changing, and updated
        // within that class, but I don't want to register observers for that for every single entity lump.
        foreach (EntityViewModel elvm in BspService.Instance.EntityLumpViewModel.Entities.Items)
            elvm.SetEntityRuleProps(Rules);
    }
}
