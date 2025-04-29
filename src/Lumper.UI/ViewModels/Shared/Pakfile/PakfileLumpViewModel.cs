namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.IO;
using System.Linq;
using DynamicData;
using DynamicData.Kernel;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Services;

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

    public void UpdateViewModelFromModel() =>
        Entries.Edit(updater =>
        {
            foreach (PakfileEntry model in _pakfile.Entries)
            {
                Optional<PakfileEntryViewModel> lookup = updater.Lookup(model.Key);
                if (lookup.HasValue && lookup is { Value: PakfileEntryViewModel vm })
                {
                    // If a job or whatever modified the entry, it'll definitely have loaded data.
                    // If VM has been loaded before, it'll have a hash.
                    if (model.HasLoadedData && vm.Hash != null && vm.Hash != model.Hash)
                        vm.OnDataUpdate();
                }
                else
                {
                    updater.AddOrUpdate(CreateEntryViewModel(model));
                }
            }

            foreach (PakfileEntryViewModel entry in Entries.Items.ToList())
            {
                if (_pakfile.Entries.All(x => x.Key != entry.Key))
                    updater.Remove(entry);
            }
        });

    private void InitEntries() => UpdateViewModelFromModel();

    private PakfileEntryViewModel CreateEntryViewModel(PakfileEntry entry) =>
        Path.GetExtension(entry.Key).Equals(".vtf", StringComparison.OrdinalIgnoreCase)
            ? new PakfileEntryVtfViewModel(entry, this)
            // Treat anything that isn't a VTF as a text file. If it's not a file we recognise as text,
            // the UI shows a warning, but does allow editing -- may be some file extensions that we
            // don't recognise but are still text.
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
