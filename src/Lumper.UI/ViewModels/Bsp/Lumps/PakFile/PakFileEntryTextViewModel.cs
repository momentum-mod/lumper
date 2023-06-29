using System.IO;
using System.Linq;
using ReactiveUI;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

public class PakFileEntryTextViewModel : PakFileEntryLeafViewModel
{
    public PakFileEntryTextViewModel(PakFileEntryBranchViewModel parent,
        PakFileEntry entry, string name)
        : base(parent, entry, name)
    {
    }

    private string _content = "";
    public string Content
    {
        get => _content;
        set
        {
            this.RaiseAndSetIfChanged(ref _content, value);
            _isModified = true;
        }
    }
    private bool _isModified = false;
    public override bool IsModified { get => _isModified; }


    private bool _isContentVisible = false;
    public bool IsContentVisible
    {
        get => _isContentVisible;
        set => this.RaiseAndSetIfChanged(ref _isContentVisible, value);
    }

    private readonly string[] KnownFileTypes =
    {
        ".txt",
        ".vbsp",
        ".vmt"
    };
    private readonly System.Text.Encoding _encoding
        = System.Text.Encoding.ASCII;


    public void ShowContent()
    {
        var stream = _entry.DataStream;
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        var sr = new StreamReader(stream, _encoding);
        Content = sr.ReadToEnd();
        //hack setting Content sets IsModified 
        _isModified = false;
        IsContentVisible = true;
    }

    public override void Open()
    {
        if (KnownFileTypes.Contains(Extension))
            ShowContent();
    }

    public override void Update()
    {
        if (IsModified)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, _encoding);
            writer.Write(Content);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            _entry.DataStream = stream;
            _isModified = false;
        }
        base.Update();
    }
}
