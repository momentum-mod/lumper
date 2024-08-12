namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lib.BSP.Struct;
using NLog;
using ReactiveUI.Fody.Helpers;

public class PakfileEntryTextViewModel(PakfileEntry entry, BspNode parent) : PakfileEntryViewModel(entry, parent)
{
    [Reactive]
    public string? Content { get; set; } = "";

    [Reactive]
    public bool IsContentLoaded { get; set; }

    private static readonly string[] KnownFileTypes = [".txt", ".vbsp", ".vmt"];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void Load(CancellationTokenSource? cts = null)
    {
        if (!IsContentLoaded && KnownFileTypes.Contains(Extension))
            LoadContent();
    }

    public void LoadContent()
    {
        try
        {
            DataStream.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(DataStream, Encoding.ASCII);
            Content = sr.ReadToEnd();
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
        var fileName = Path.Combine(Path.GetTempPath(), $"{Name}-{Guid.NewGuid()}{Extension}");

        try
        {
            await using var fileStream = new FileStream(fileName, FileMode.CreateNew);
            DataStream.Seek(0, SeekOrigin.Begin);
            await DataStream.CopyToAsync(fileStream);
            fileStream.Flush();
            await Program.Desktop.MainWindow!.Launcher.LaunchUriAsync(new Uri(fileName));
        }
        catch (IOException ex)
        {
            Logger.Error(ex, "Failed to create temporary file");
        }
    }

    public override void UpdateModel()
    {
        if (!IsModified)
            return;

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII);
        writer.AutoFlush = true;
        writer.Write(Content);
        stream.Seek(0, SeekOrigin.Begin);
        DataStream = stream;
    }
}
