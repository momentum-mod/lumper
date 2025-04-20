namespace Lumper.UI.ViewModels.Shared;

using System;

/// <summary>
/// A ViewModel responsible for reactively exposing the data in a specific lump.
///
/// The class must be disposable, and should clear the SourceList/SourceCache of its contents during.
///
/// LumpViewModels implement both "push" and "pull" between ViewModel and Model. During saving,
/// before running jobs, etc., any data stored on the viewmodel needs to be push to the model,
/// and after a job has been ran, the changes pulled back.
/// </summary>
public abstract class LumpViewModel : BspNode, IDisposable
{
    /// <summary>
    /// Scan the underlying model for changes and update on the viewmodel.
    ///
    /// Use this for Jobs that affect this lump. Jobs are part of Lumper.Lib so have no effect
    /// on the viewmodel, we have figure them out programmatically.
    /// </summary>
    public abstract void PullChangesFromModel();

    public abstract void Dispose();
}
