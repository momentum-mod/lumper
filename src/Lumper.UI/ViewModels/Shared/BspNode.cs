namespace Lumper.UI.ViewModels.Shared;

using Lumper.UI.Services;
using ReactiveUI.Fody.Helpers;

/// <summary>
/// Very generic data structure for viewmodel representing properties on the BSP.
/// </summary>
public abstract class BspNode : ViewModel
{
    [Reactive]
    public bool IsModified { get; set; }

    /// <summary>
    /// Update the underlying model, and calls any child nodes to do the same. When the BSP is about to be saved,
    /// this method recursives down BSP nodes and pushes values on the viewmodel onto the model, for viewmodels
    /// whose data is not directly stored on the model (e.g. pakfile lump).
    ///
    /// For viewmodels that immediately push changes to their model (e.g. entities), this can be left as a noop.
    /// </summary>
    public virtual void PushChangesToModel() { }

    public virtual void MarkAsModified()
    {
        if (IsModified)
            return;

        IsModified = true;
        BspService.Instance.MarkAsModified();
    }
}

public abstract class HierarchicalBspNode(BspNode parent) : BspNode
{
    public BspNode Parent { get; } = parent;

    public override void MarkAsModified()
    {
        if (IsModified)
            return;

        IsModified = true;
        Parent.MarkAsModified();
        BspService.Instance.MarkAsModified();
    }
}
