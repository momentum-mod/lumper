namespace Lumper.UI.ViewModels.Shared;

using System.Runtime.CompilerServices;
using Entity;
using Models.Matchers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;

/// <summary>
/// Very generic data structure for viewmodel representing properties on the BSP.
/// </summary>
public abstract class BspNode : ViewModel
{
    [Reactive]
    public bool IsModified { get; set; }

    /// <summary>
    /// This is essentially a wrapper around this.RaiseAndSetIfChanged that also calls MarkAsModified.
    ///
    /// See <see cref="EntityPropertyViewModel"/> for examples.
    ///
    /// Nodes (which are viewmodels) store copies of properties stored on the models, rather than
    /// referencing the values themselves. I played around with both approaches, going far as implementing
    /// a reflection-based version of this method passing property accessor lambdas around, but ended up
    /// going with storing copies.
    ///
    /// The pakfile lump inevitably has to have a bunch of custom behaviour since we want to let the user
    /// manipulate that lump without constantly modifying the underlying zip archive, so that'd
    /// need some kind of UpdateModel() method anyway.
    ///
    /// Also Jobs modify the models directly, making detecting vm changes difficult - we'd need to
    /// clone the whole model (e.g. entity lump for stripperjob) before running then, then compare with
    /// result to handle MarkAsModified.
    ///
    /// So, just using copies everywhere and UpdateModel() approach seems okay, and avoids gnarly
    /// ReactiveUI-style lambdas + reflection, that's why this method works the way it does.
    ///
    /// </summary>
    /// <param name="backingField">Field to update</param>
    /// <param name="newValue">New value</param>
    /// <param name="propertyName"></param>
    protected bool UpdateField<T>(
        ref T backingField,
        T newValue,
        [CallerMemberName] string? propertyName = null)
    {
        if ((backingField is null && newValue is null) || (backingField is not null && backingField.Equals(newValue)))
            return false;

        backingField = newValue;
        this.RaisePropertyChanged(propertyName);
        MarkAsModified();
        return true;
    }

    /// <summary>
    /// Update the underlying model, and calls any child nodes to do the same. When the BSP is about to be saved,
    /// this method recursives down bsp nodes and pushes values on the viewmodel onto the model.
    /// </summary>
    public abstract void UpdateModel();

    public virtual void MarkAsModified()
    {
        if (IsModified)
            return;

        IsModified = true;
        BspService.Instance.MarkAsModified();
    }
}

public abstract class HierarchicalBspNode : BspNode
{
    public required BspNode Parent { get; init; }

    public override void MarkAsModified()
    {
        if (IsModified)
            return;

        IsModified = true;
        Parent.MarkAsModified();
        BspService.Instance.MarkAsModified();
    }
}

public abstract class MatchableBspNode : HierarchicalBspNode
{
    public abstract bool Match(Matcher matcher);
}
