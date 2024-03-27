namespace Lumper.UI.ViewModels;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Services;

/// <summary>
/// TODO
/// </summary>
public abstract class BspNodeBase : ViewModelBase
{
    [Reactive]
    protected bool IsModified { get; private set; } = false;

    protected void UpdateField<T>(
        ref T backingField,
        T newValue,
        [CallerMemberName] string? propertyName = null)
    {
        this.RaiseAndSetIfChanged(ref backingField, newValue, propertyName);
        MarkAsModified();
    }

    protected void MarkAsModified()
    {
        if (IsModified)
            return;

        IsModified = true;
        ActiveBspService.Instance.MarkAsModified();
    }

    protected abstract ValueTask<bool> Match(Matcher matcher, CancellationToken? _);
}
