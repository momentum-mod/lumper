namespace Lumper.UI.ViewModels.Shared;

using System;

/// <summary>
/// A viewmodel responsible for (reactively) exposing the data in a specific lump.
///
/// The class must be disposable, and should clear the SourceList/SourceCache of its contents during.
/// </summary>
public interface ILumpViewModel : IDisposable
{
    public void UpdateModel();
}
