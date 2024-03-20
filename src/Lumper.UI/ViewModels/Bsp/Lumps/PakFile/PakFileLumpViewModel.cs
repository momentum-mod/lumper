namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
using System;
using System.Collections.Generic;
using System.IO;
using Lumper.Lib.BSP.Lumps.BspLumps;
using SharpCompress.Archives.Zip;

/// <summary>
///     ViewModel for the PakFile
/// </summary>
public class PakFileLumpViewModel : LumpBase
{
    public PakFileEntryBranchViewModel EntryRoot { get; }
    private readonly PakFileLump _lump;
    public PakFileLumpViewModel(BspViewModel parent, PakFileLump pakFileLump)
        : base(parent)
    {
        _lump = pakFileLump;
        EntryRoot = new(this, pakFileLump);

        InitializeNodeChildrenObserver(EntryRoot._entries);
    }

    public override string NodeName => "PakFile";
    public List<PakFileEntryLeafViewModel> ZipEntries { get; } = [];

    public override BspNodeBase? ViewNode => this;

    public void Import(string path)
    {
        var importDir = new DirectoryInfo(path);
        if (!importDir.Exists)
            throw new DirectoryNotFoundException(path);

        var zip = ZipArchive.Create();

        //hack because the clear in CreateNodes doesn't work
        ZipEntries.Clear();
        AddFilesRecusive(importDir, importDir, zip);
        _lump.Zip = zip;
        EntryRoot.CreateNodes(_lump.Entries);
    }

    private static void AddFilesRecusive(DirectoryInfo dir,
                                  DirectoryInfo importDir,
                                  ZipArchive zip)
    {
        foreach (FileInfo file in dir.GetFiles())
        {
            using var fs = new FileStream(file.FullName, FileMode.Open);
            var mem = new MemoryStream();
            fs.CopyTo(mem);
            if (!file.FullName.StartsWith(importDir.FullName))
            {
                throw new InvalidDataException(
                    "how did you do that?" +
                    $"'{importDir.FullName}' -> '{file.FullName}'");
            }

            var entryPath = file.FullName[importDir.FullName.Length..];
            //using the filestream instead of the memorystream here means
            //we have to keep all the files open .. if we close them, we can't save the bsp
            //because we can't create the zip from closed streams
            zip.AddEntry(entryPath, mem);
        }

        foreach (DirectoryInfo subdir in dir.GetDirectories())
        {
            AddFilesRecusive(subdir, importDir, zip);
        }
    }

    public void Export(string path)
    {
        var exportDir = new DirectoryInfo(path);
        if (!exportDir.Exists)
            throw new DirectoryNotFoundException(path);
        if (exportDir.GetFiles().Length != 0
            || exportDir.GetDirectories().Length != 0)
        {
            Console.WriteLine("Refusing to export to a directory containing stuff");
            //todo messagebox .. but not here because of dependencies?
            //option to delete files?
            return;
        }

        SharpCompress.Readers.IReader reader;
        //todo maybe don't trycatch here
        //after import we can't export .. but what do we have to check here?
        try
        {
            reader = _lump.Zip.ExtractAllEntries();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to extract zip: " + ex.Message);
            return;
        }
        while (reader.MoveToNextEntry())
        {
            FileInfo fi = new(Path.Join(exportDir.FullName, reader.Entry.Key));
            var name = fi.Directory?.FullName;
            if (!string.IsNullOrWhiteSpace(name))
            {
                Directory.CreateDirectory(name);
                using var fstream = new FileStream(fi.FullName, FileMode.Create);
                reader.WriteEntryTo(fstream);
            }
        }
    }

    public void AddFile(string key, Stream stream) => EntryRoot.AddFile(key, stream);
}
