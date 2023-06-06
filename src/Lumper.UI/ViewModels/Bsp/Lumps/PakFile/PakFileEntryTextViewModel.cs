using System.IO;
using System.Collections.Generic;
using SharpCompress.Archives.Zip;
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

    private readonly System.Text.Encoding _encoding
        = System.Text.Encoding.ASCII;

    public override void Open()
    {
        using var mem = new MemoryStream();
        _entry.DataStream.CopyTo(mem);
        mem.Seek(0, SeekOrigin.Begin);
        var sr = new BinaryReader(mem);
        //todo async
        byte[] b = sr.ReadBytes((int)mem.Length);
        Content = _encoding.GetString(b);
        //hack setting Content sets IsModified 
        _isModified = false;
    }

    public override void Update()
    {
        if (IsModified)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(_encoding.GetBytes(Content));
            stream.Seek(0, SeekOrigin.Begin);
            _entry.DataStream = stream;
            _isModified = false;
        }
        base.Update();
    }
}
