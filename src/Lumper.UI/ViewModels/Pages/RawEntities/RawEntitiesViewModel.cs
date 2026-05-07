namespace Lumper.UI.ViewModels.Pages.RawEntities;

using System.IO;
using System.Reactive.Linq;
using AvaloniaEdit;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Entity;
using Lumper.UI.Views.Pages.RawEntities;
using NLog;
using ReactiveUI;

public sealed class RawEntitiesViewModel : ViewModelWithView<RawEntitiesViewModel, RawEntitiesView>
{
    private TextEditor? _editor;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Called when view is ready to have the entity lump dumped into it.
    /// We're handling a specific TextEditor instance for this page, so don't
    /// mind viewmodel being quite tightly coupled to the view.
    /// </summary>
    public void LoadEntityLump(EntityLumpViewModel? entLump, TextEditor editor)
    {
        if (entLump is null)
            return;

        _editor = editor;

        using MemoryStream stream = entLump.GetStream();
        // Remove NULL byte from end of stream, don't want user to see it
        stream.SetLength(stream.Length - 1);

        editor.IsVisible = true;
        editor.Load(stream);

        BspService.Instance.EntityLumpViewModel!.IsEditingStream = true;
        BspService.Instance.EntityLumpViewModel!.RawEntitiesViewModel = this;
    }

    /// <summary>
    /// Flushes any pending edits in the text editor to the entity lump.
    /// Called when the BSP is being saved whilst this page is visible.
    /// </summary>
    public void SaveEntityLump()
    {
        if (BspService.Instance.EntityLumpViewModel is not { } entLump)
            return;

        if (_editor is not { IsModified: true })
            return;

        BspService.Instance.IsLoading = true;
        BspService.Instance.MarkAsModified();

        MemoryStream stream = new();
        _editor.Save(stream);

        stream.Seek(0, SeekOrigin.End);
        stream.Write("\0"u8); // Add NULL terminator back

        Observable.Start(() => entLump.UpdateFromStream(stream), RxApp.TaskpoolScheduler);

        Logger.Info("Updated entity lump with raw entity data");

        BspService.Instance.IsLoading = false;
    }

    /// <summary>
    /// Saves any pending edits and tears down editor state. Called when navigating
    /// away from the raw entities page.
    /// </summary>
    public void CloseEntityLump()
    {
        SaveEntityLump();

        if (BspService.Instance.EntityLumpViewModel is { } entLump)
        {
            entLump.IsEditingStream = false;
            entLump.RawEntitiesViewModel = null;
        }

        _editor = null;
    }
}
