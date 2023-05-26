using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SharpCompress.Archives.Zip;
using ReactiveUI;
using VTFLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

public class PakFileEntryTextViewModel : PakFileEntryLeafViewModel
{
    public PakFileEntryTextViewModel(PakFileEntryBranchViewModel parent,
        ZipArchiveEntry entry, string name)
        : base(parent, entry, name)
    { }

    private string _content = "";
    public string Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    public override void Open()
    {
        base.Open();
        var sr = new StreamReader(Stream);
        //todo async
        Content = sr.ReadToEnd();
    }
}
