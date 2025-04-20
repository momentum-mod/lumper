namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Views.Shared.Pakfile;
using NLog;
using ReactiveUI.Fody.Helpers;

public class PakfileEntryTextViewModel : PakfileEntryViewModel
{
    [Reactive]
    public string? Content { get; set; } = "";

    [Reactive]
    public bool IsContentLoaded { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public PakfileEntryTextViewModel(PakfileEntry entry, BspNode parent)
        : base(entry, parent) => RegisterView<PakfileEntryTextViewModel, PakfileEntryTextView>();

    public override void Load(CancellationTokenSource? cts = null)
    {
        if (!IsContentLoaded && PakfileLump.TextFileTypes.Contains(Extension))
            LoadContent();
    }

    public void LoadContent()
    {
        try
        {
            Content = BspFile.Encoding.GetString(GetData());
        }
        catch
        {
            LogManager.GetCurrentClassLogger().Warn("Failed to load pakfile entry!");
            Content = null;
        }

        IsContentLoaded = true;
    }

    public async Task OpenExternal()
    {
        string fileName = Path.Combine(Path.GetTempPath(), $"{Name}-{Guid.NewGuid()}{Extension}");

        try
        {
            await using var fileStream = new FileStream(fileName, FileMode.CreateNew);
            fileStream.Write(GetData());
            fileStream.Flush();
            await Program.MainWindow.Launcher.LaunchUriAsync(new Uri(fileName));
        }
        catch (IOException ex)
        {
            Logger.Error(ex, "Failed to create temporary file");
        }
    }

    public override void PushChangesToModel()
    {
        if (!IsModified || !IsContentLoaded)
            return;

        UpdateData(BspFile.Encoding.GetBytes(Content ?? ""));
    }

    public override void OnDataUpdate()
    {
        // Run first so vm hash is set update and equal to model hash, otherwise
        // LoadContent -> GetData -> OnDataUpdate calls will stack overflow.
        base.OnDataUpdate();

        // If we had data loaded into Content previously and the model data updates, update Content.
        if (IsContentLoaded)
            LoadContent();
    }
}
