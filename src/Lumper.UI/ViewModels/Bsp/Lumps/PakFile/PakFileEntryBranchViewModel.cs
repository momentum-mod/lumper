namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;
using ReactiveUI;

public class PakFileEntryBranchViewModel : PakFileEntryBaseViewModel
{
    public PakFileEntryBranchViewModel(PakFileLumpViewModel parent, PakFileLump pakFile)
        : base(parent, "root")
    {
        _pakFile = pakFile;
        _pakFileViewModel = parent;
        CreateNodes(pakFile.Entries);
        Init();
    }

    public PakFileEntryBranchViewModel(PakFileEntryBranchViewModel parent,
        string name)
        : base(parent, name)
    {
        _pakFile = parent._pakFile;
        _pakFileViewModel = parent._pakFileViewModel;
        Init();
    }

    private void Init() => _entries
        .Connect()
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(x =>
        {
            //todo 'clear' doesn't work with this 
            //its a x.Item.Rang and x.Item.Current is null
            IEnumerable<Change<PakFileEntryBaseViewModel>> list = x.Where(x => x.Item.Current is PakFileEntryLeafViewModel);

            IEnumerable<PakFileEntryLeafViewModel> addList
                = list.Where(x => x.Item.Reason is ListChangeReason.Add
                        or ListChangeReason.AddRange)
                    .Select(x => (PakFileEntryLeafViewModel)x.Item.Current);
            IEnumerable<PakFileEntryLeafViewModel> deleteList
                = list.Where(x => x.Item.Reason is ListChangeReason.Remove
                        or ListChangeReason.RemoveRange
                        or ListChangeReason.Clear)
                    .Select(x => (PakFileEntryLeafViewModel)x.Item.Current);
            if (deleteList.Any())
                _pakFileViewModel.ZipEntries.RemoveMany(deleteList);
            if (addList.Any())
                _pakFileViewModel.ZipEntries.AddRange(addList);

        });

    private readonly PakFileLump _pakFile;
    private readonly PakFileLumpViewModel _pakFileViewModel;
    public override BspNodeBase? ViewNode => this;

    public void CreateNodes(IEnumerable<PakFileEntry> entries)
    {
        _entries.Clear();
        foreach (PakFileEntry entry in entries)
        {
            CreateNodes(entry);
        }
    }

    private PakFileEntryBranchViewModel AddBranch(string name)
    {
        PakFileEntryBaseViewModel? dir =
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
            if (name.ToLower(System.Globalization.CultureInfo.CurrentCulture).EndsWith(".vtf"))
                _entries.Add(new PakFileEntryVtfViewModel(this, entry, name));
            else
                _entries.Add(new PakFileEntryTextViewModel(this, entry, name));
        }
    }

    public PakFileEntryBranchViewModel CreatePathRoot(string entryKey, out string name) => _pakFileViewModel.EntryRoot.CreatePath(entryKey, out name);

    private PakFileEntryBranchViewModel CreatePath(string entryKey, out string name, int index = 0)
    {
        var path = entryKey.Split('/');

        var isDir = index < path.Length - 1;
        name = path[index];
        if (isDir)
        {
            PakFileEntryBranchViewModel dir = AddBranch(name);
            return dir.CreatePath(entryKey, out name, index + 1);
        }
        else
        {
            return this;
        }
    }

    public void CreateNodes(PakFileEntry entry)
    {
        PakFileEntryBranchViewModel dir = CreatePath(entry.Key, out var name);
        dir.AddLeaf(name, entry);
    }

    public void MoveNode(PakFileEntryLeafViewModel leaf)
    {
        _entries.Add(leaf);
        if (leaf.Parent is PakFileEntryBranchViewModel branch)
            branch._entries.Remove(leaf);
    }

    private bool DeleteEmptyNodes()
    {
        var hasLeafs = false;
        List<PakFileEntryBranchViewModel> deleteList = [];
        foreach (PakFileEntryBaseViewModel entry in _entries.Items)
        {
            if (entry is PakFileEntryBranchViewModel branch)
            {
                var entryHasLeafs = branch.DeleteEmptyNodes();
                if (entryHasLeafs)
                    hasLeafs = entryHasLeafs;
                else
                    deleteList.Add(branch);
            }
            else if (entry is PakFileEntryLeafViewModel)
            {
                hasLeafs = true;
            }
            else
            {
                throw new InvalidDataException(
                    $"what is this doing here {entry.GetType().Name}");
            }
        }
        foreach (PakFileEntryBranchViewModel branch in deleteList)
            _entries.Remove(branch);
        return hasLeafs;
    }

    public void AddFile(string key, Stream stream)
    {
        if (key.Contains('/'))
        {
            throw new NotSupportedException(
                "no path here for now .. only the directory name");
        }

        var entry = new PakFileEntry(key, stream);
        _pakFile.Entries.Add(entry);
        AddLeaf(key, entry);
    }

    public void AddDir(string key)
    {
        if (key.Contains('/'))
        {
            throw new NotSupportedException(
                "no path here for now .. only the directory name");
        }

        AddBranch(key);
    }

    //todo close the open tab on delete
    public void Delete()
    {
        foreach (PakFileEntryBaseViewModel entry in _entries.Items)
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
