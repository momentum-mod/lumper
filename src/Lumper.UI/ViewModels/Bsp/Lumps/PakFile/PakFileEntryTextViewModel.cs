using System.IO;
using System.Collections.Generic;
using SharpCompress.Archives.Zip;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

public class PakFileEntryTextViewModel : PakFileEntryLeafViewModel
{
    public PakFileEntryTextViewModel(PakFileEntryBranchViewModel parent,
        ZipArchiveEntry entry, string name)
        : base(parent, entry, name)
    {
        //todo don't call this here
        Open();
    }

    private string _content = "";
    public string Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }
    private bool _isModified = false;
    public override bool IsModified { get => _isModified; }

    private readonly System.Text.Encoding _encoding = System.Text.Encoding.Latin1;

    public void Open()
    {
        using var mem = new MemoryStream();
        Stream.CopyTo(mem);
        mem.Seek(0, SeekOrigin.Begin);
        var sr = new BinaryReader(mem);
        //todo async
        byte[] b = sr.ReadBytes((int)mem.Length);
        Content = _encoding.GetString(b);
    }

    public override void Save(ZipArchive zip, ref List<Stream> streams)
    {
        //todo
        _isModified = true;
        if (IsModified)
        {
            Stream = new MemoryStream();
            var writer = new BinaryWriter(Stream);
            writer.Write(_encoding.GetBytes(Content));
            Stream.Seek(0, SeekOrigin.Begin);
            _isModified = false;
        }
        //else
        //    Stream = _entry.OpenEntryStream();
        base.Save(zip, ref streams);
    }
}
