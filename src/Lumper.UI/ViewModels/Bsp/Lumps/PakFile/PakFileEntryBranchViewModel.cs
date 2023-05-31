using System.Linq;
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
        foreach (var entry in pakFile.Entries)
        {
            CreateNodes(entry);
        }
        InitializeNodeChildrenObserver();
    }

    public PakFileEntryBranchViewModel(PakFileEntryBranchViewModel parent, string name)
        : base(parent, name)
    { }

    public override BspNodeBase? ViewNode => null;

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
                dir = new PakFileEntryBranchViewModel(this, name);
                _entries.Add(dir);
            }
            ((PakFileEntryBranchViewModel)dir).CreateNodes(entry, index + 1);
        }
        else
        {
            if (name.ToLower().EndsWith(".vtf"))
                _entries.Add(new PakFileEntryVtfViewModel(this, entry, name));
            else
                _entries.Add(new PakFileEntryTextViewModel(this, entry, name));
        }
    }
}
