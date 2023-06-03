using System.IO;
using System.Linq;
using System.Collections.Generic;
using DynamicData;
using SharpCompress.Archives.Zip;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
public class PakFileEntryBranchViewModel : PakFileEntryBaseViewModel
{
    public PakFileEntryBranchViewModel(PakFileLumpViewModel parent, PakFileLump pakFile)
    : base(parent, "root")
    {
        _pakFile = pakFile;
        CreateNodes(pakFile.Entries);
        InitializeNodeChildrenObserver();
    }

    public PakFileEntryBranchViewModel(PakFileEntryBranchViewModel parent,
                                       string name,
                                       string path)
        : base(parent, name)
    {
        Path = path;
    }

    public string Path { get; }
    private readonly PakFileLump _pakFile;

    public override BspNodeBase? ViewNode => this;

    private void CreateNodes(IEnumerable<PakFileEntry> entries)
    {
        foreach (var entry in entries)
        {
            CreateNodes(entry);
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
            var dir =
                _entries.AsObservableList().Items
                .FirstOrDefault(x => x is PakFileEntryBranchViewModel branch
                                  && branch._name == name, null);
            if (dir is null)
            {
                dir = new PakFileEntryBranchViewModel(
                    this,
                    name,
                    string.Join("/", path.Take(index + 1)) + "/");
                _entries.Add(dir);
            }
            ((PakFileEntryBranchViewModel)dir).CreateNodes(entry, index + 1);
        }
        else
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
        if (Parent is PakFileEntryBranchViewModel branch)
        {
            branch.AddFile(_name + "/" + key, stream);
        }
        else if (Parent is PakFileLumpViewModel)
        {
            _pakFile.Entries.Add(new PakFileEntry(key, stream));

            CreateNodes(_pakFile.Entries);
            DeleteEmptyNodes();
        }
    }
}
