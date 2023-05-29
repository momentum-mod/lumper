using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DynamicData;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Lumps;
using Lumper.UI.Models;
using SharpCompress.Archives.Zip;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

/// <summary>
///     ViewModel for the PakFile
/// </summary>
public class PakFileLumpViewModel : LumpBase
{
    //private readonly SourceList<PakFileEntryViewModel> _entries = new();
    private readonly PakFileEntryBranchViewModel _entryRoot;
    private readonly PakFileLump _lump;
    public PakFileLumpViewModel(BspViewModel parent, PakFileLump pakFileLump)
        : base(parent)
    {
        _entryRoot = new(this, pakFileLump.Zip);
        _lump = pakFileLump;

        InitializeNodeChildrenObserver(_entryRoot._entries);
    }

    public override string NodeName => "PakFile";

    public override BspNodeBase? ViewNode => this;

    public void Import(string path, bool compress = false)
    {
        var importDir = new DirectoryInfo(path);
        if (!importDir.Exists)
            throw new DirectoryNotFoundException(path);

        var zip = ZipArchive.Create();

        List<FileStream> fileStreams = new();
        AddFilesRecusive(fileStreams, importDir, importDir, zip);

        _lump.SetZipArchive(zip, compress);

        foreach (var fs in fileStreams)
        {
            fs.Dispose();
        }
    }

    private static void AddFilesRecusive(List<FileStream> fileStreams,
                                  DirectoryInfo dir,
                                  DirectoryInfo importDir,
                                  ZipArchive zip)
    {
        foreach (var file in dir.GetFiles())
        {
            var fs = new FileStream(file.FullName, FileMode.Open);
            fileStreams.Add(fs);
            /*string entryPath = Path.GetRelativePath(importDir.FullName, file.FullName);
            if (entryPath.StartsWith("./"))
                entryPath = entryPath.Substring(2);*/
            if (!file.FullName.StartsWith(importDir.FullName))
                throw new InvalidDataException(
                    "who did you do that?" +
                    $"'{importDir.FullName}' -> '{file.FullName}'");

            string entryPath = file.FullName[importDir.FullName.Length..];
            zip.AddEntry(entryPath, fs);
        }
        foreach (var subdir in dir.GetDirectories())
        {
            AddFilesRecusive(fileStreams, subdir, importDir, zip);
        }
    }

    public void Export(string path)
    {
        var exportDir = new DirectoryInfo(path);
        if (!exportDir.Exists)
            throw new DirectoryNotFoundException(path);

        var reader = _lump.Zip.ExtractAllEntries();
        while (reader.MoveToNextEntry())
        {
            FileInfo fi = new(Path.Join(exportDir.FullName, reader.Entry.Key));
            Directory.CreateDirectory(fi.Directory.FullName);
            using var fstream = new FileStream(fi.FullName, FileMode.Create);
            reader.WriteEntryTo(fstream);
        }
    }

    public async void Save()
    {
        //todo make a new one for now .. ugly and slow?
        var zip = ZipArchive.Create();
        try
        {
            var streams = new List<Stream>();
            _entryRoot.Save(zip, ref streams);
            //todo
            bool compress = false;
            _lump.SetZipArchive(zip, compress);

            //close all new streams we had to make
            foreach (var stream in streams)
            {
                stream.Close();
                stream.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}