using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData;
using SharpCompress.Archives.Zip;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;
using ReactiveUI;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public class PakFileEntryBranchViewModel : PakFileEntryBaseViewModel
{
    public PakFileEntryBranchViewModel(PakFileLumpViewModel parent, PakFileLump pakFile)
    : base(parent, "root")
    {
        _pakFile = pakFile;
        CreateNodes(pakFile.Entries);
    }

    public PakFileEntryBranchViewModel(PakFileEntryBranchViewModel parent,
                                       string name)
        : base(parent, name)
    {
        _pakFile = parent._pakFile;
    }

    private readonly PakFileLump _pakFile;

    public override BspNodeBase? ViewNode => this;

    private void CreateNodes(IEnumerable<PakFileEntry> entries)
    {
        foreach (var entry in entries)
        {
            CreateNodes(entry);
        }
    }

    private PakFileEntryBranchViewModel AddBranch(string name)
    {
        var dir =
            _entries.AsObservableList().Items
            .FirstOrDefault(x => x is PakFileEntryBranchViewModel branch
                                && branch.Name == name, null);
        if (dir is null)
        {
            dir = new PakFileEntryBranchViewModel(this, name);
            _entries.Add(dir);
        }
        return (PakFileEntryBranchViewModel)dir;
    }


    private void AddLeaf(string name, PakFileEntry entry)
    {
        if (!_entries.Items.Any(
                x => x is PakFileEntryLeafViewModel leaf
                     && leaf.Entry == entry))
        {
            if (name.ToLower().EndsWith(".vtf"))
                _entries.Add(new PakFileEntryVtfViewModel(this, entry, name));
            else
                _entries.Add(new PakFileEntryTextViewModel(this, entry, name));
        }
    }
    private void CreateNodes(PakFileEntry entry, int index = 0)
    {
        var path = entry.Key.Split('/');
        bool isDir;
        if (index == path.Length - 1)
            isDir = false;
        else
            isDir = true;
        string name = path[index];
        if (isDir)
        {
            var dir = AddBranch(name);
            dir.CreateNodes(entry, index + 1);
        }
        else
        {
            AddLeaf(name, entry);
        }
    }

    private bool DeleteEmptyNodes()
    {
        bool hasLeafs = false;
        List<PakFileEntryBranchViewModel> deleteList = new();
        foreach (var entry in _entries.Items)
        {
            if (entry is PakFileEntryBranchViewModel branch)
            {
                bool entryHasLeafs = branch.DeleteEmptyNodes();
                if (entryHasLeafs)
                    hasLeafs = entryHasLeafs;
                else
                    deleteList.Add(branch);
            }
            else if (entry is PakFileEntryLeafViewModel)
                hasLeafs = true;
            else
                throw new InvalidDataException($"what is this doing here {entry.GetType().Name}");
        }
        foreach (var branch in deleteList)
            _entries.Remove(branch);
        return hasLeafs;
    }

    public void AddFile(string key, Stream stream)
    {
        if (key.Contains("/"))
            throw new NotSupportedException(
                "no path here for now .. only the directory name");
        var entry = new PakFileEntry(key, stream);
        _pakFile.Entries.Add(entry);
        AddLeaf(key, entry);
        //CreateNodes(_pakFile.Entries);
        //DeleteEmptyNodes();
    }

    public void AddDir(string key)
    {
        if (key.Contains("/"))
            throw new NotSupportedException(
                "no path here for now .. only the directory name");
        var dir = AddBranch(key);
    }

    //todo close the open tab on delete
    public void Delete()
    {
        foreach (var entry in _entries.Items)
        {
            if (entry is PakFileEntryBranchViewModel branch)
                branch.Delete();
            else if (entry is PakFileEntryLeafViewModel leaf)
                _pakFile.Entries.Remove(leaf.Entry);
        }
        _entries.Clear();
        if (Parent is PakFileEntryBranchViewModel parentBranch)
            parentBranch._entries.Remove(this);
    }
    public void Delete(PakFileEntryLeafViewModel leaf)
    {
        _pakFile.Entries.Remove(leaf.Entry);
        _entries.Remove(leaf);
    }
}
