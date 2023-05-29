using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SharpCompress.Archives.Zip;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public class PakFileEntryLeafViewModel : PakFileEntryBaseViewModel
//, IDisposable
{
    private readonly ZipArchiveEntry? _entry;

    public PakFileEntryLeafViewModel(PakFileEntryBranchViewModel parent,
        ZipArchiveEntry entry, string name)
        : base(parent, name)
    {
        _entry = entry;
        OpenEntryStream();
    }

    ~PakFileEntryLeafViewModel()
    {
        Close();
    }

    private Stream? _stream = null;
    public Stream? Stream
    {
        get { return _stream; }
        protected set
        {
            Close();
            _stream = value;
        }
    }

    public void OpenEntryStream()
    {
        Stream = _entry.OpenEntryStream();
    }

    public void Close()
    {
        if (_stream is not null)
        {
            _stream.Close();
            _stream.Dispose();
            _stream = null;
        }
    }

    //todo this is probably not stay like this
    public override void Save(ZipArchive zip, ref List<Stream> streams)
    {
        try
        {
            var stream = Stream;
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            else if (!IsModified)
            {
                var mem = new MemoryStream();
                OpenEntryStream();
                Stream.CopyTo(mem);
                stream = mem;
                streams.Add(mem);
            }
            else if (stream.Position >= stream.Length)
                throw new EndOfStreamException(
                    "Stream is not seekable, was modified  " +
                    "and there is nothing to read .. what did you do?");
            //hack leak .. if I dispose it, setziparchive failes
            //var mem = new MemoryStream();
            //Stream.CopyTo(mem);
            //long test = mem.Length;
            //mem.Seek(0, SeekOrigin.Begin);
            zip.AddEntry(_entry.Key, stream);
            base.Save(zip, ref streams);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }

}
