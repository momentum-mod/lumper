using System;
using System.IO;
using System.Linq;
using SharpCompress.Archives.Zip;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public class PakFileEntryLeafViewModel : PakFileEntryBaseViewModel
{
    protected readonly ZipArchiveEntry? _entry;

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

    //todo this is probably not stay like this
    public override void Save(ZipArchive zip)
    {
        try
        {
            //hack leak .. if I dispose it, setziparchive failes
            var mem = new MemoryStream();
            Stream.CopyTo(mem);
            long test = mem.Length;
            mem.Seek(0, SeekOrigin.Begin);
            zip.AddEntry(_entry.Key, mem);
            base.Save(zip);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }

}
