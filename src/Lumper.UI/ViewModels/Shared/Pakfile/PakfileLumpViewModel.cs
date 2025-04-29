namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicData;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Entity;

public sealed class PakfileLumpViewModel : BspNode, ILumpViewModel
{
    private readonly PakfileLump _pakfile;

    public SourceCache<PakfileEntryViewModel, string> Entries { get; } = new(entry => entry.Key);

    public PakfileLumpViewModel() => throw new NotImplementedException();

    public PakfileLumpViewModel(BspFile bsp)
    {
        BspService.Instance.ThrowIfNoLoadedBsp();

        _pakfile = bsp.GetLump<PakfileLump>();

        InitEntries();
    }

    public void UpdateViewModelFromModel(bool checkIfModified) =>
        Entries.Edit(updater =>
        {
            foreach (PakfileEntry entry in _pakfile.Entries.OrderBy(x => new FileInfo(x.Key).Name))
            {
                if (entry.IsModified || !checkIfModified)
                    updater.AddOrUpdate(CreateEntryViewModel(entry));
            }

            foreach (PakfileEntryViewModel entry in Entries.Items.ToList())
            {
                if (_pakfile.Entries.All(x => x.Key != entry.Key))
                    updater.Remove(entry);
            }
        });

    private void InitEntries() => UpdateViewModelFromModel(false);

    private PakfileEntryViewModel CreateEntryViewModel(PakfileEntry entry) =>
        Path.GetExtension(entry.Key).Equals(".vtf", StringComparison.OrdinalIgnoreCase)
            ? new PakfileEntryVtfViewModel(entry, this)
            : new PakfileEntryTextViewModel(entry, this);

    public PakfileEntryViewModel AddEntry(
        string key,
        Stream stream,
        ISourceUpdater<PakfileEntryViewModel, string>? updater = null
    )
    {
        var entry = new PakfileEntry(_pakfile, key, stream) { IsModified = true };
        _pakfile.Entries.Add(entry);

        PakfileEntryViewModel vm = CreateEntryViewModel(entry);
        if (updater is not null)
            updater.AddOrUpdate(vm);
        else
            Entries.AddOrUpdate(vm);

        MarkAsModified();
        return vm;
    }

    public void DeleteEntry(PakfileEntryViewModel entry, ISourceUpdater<PakfileEntryViewModel, string>? updater = null)
    {
        _pakfile.Entries.Remove(entry.BaseEntry);

        if (updater is not null)
            updater.Remove(entry);
        else
            Entries.Remove(entry);

        MarkAsModified();
    }

    public override void UpdateModel()
    {
        if (!IsModified)
            return;

        _pakfile.IsModified = true;
        foreach (PakfileEntryViewModel item in Entries.Items)
            item.UpdateModel();
    }

    public void Dispose()
    {
        Entries.Clear();
        Entries.Dispose();
    }
}
