using System;
using System.IO;
using System.Linq;
using SharpCompress.Archives.Zip;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public class PakFileEntryLeafViewModel : PakFileEntryBaseViewModel
{
    private readonly ZipArchiveEntry? _entry;

    public PakFileEntryLeafViewModel(PakFileEntryBranchViewModel parent,
        ZipArchiveEntry entry, string name)
        : base(parent, name)
    {
        _entry = entry;
        //todo don't call this here
        Open();
    }

    public Stream? Stream { get; protected set; }

    public virtual void Open()
    {
        Stream = _entry.OpenEntryStream();
    }

    public void Close()
    {
        if (Stream is not null)
        {
            Stream.Close();
            Stream.Dispose();
            Stream = null;
        }
    }

    public void Save()
    {
        throw new NotImplementedException("todo save");
        Close();
    }

}
