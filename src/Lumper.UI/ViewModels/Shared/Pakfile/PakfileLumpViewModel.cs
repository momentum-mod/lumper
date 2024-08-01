namespace Lumper.UI.ViewModels.Shared.Pakfile;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicData;
using Entity;
using Lib.BSP;
using Lib.BSP.Lumps.BspLumps;
using Lib.BSP.Struct;
using NLog;
using Services;

public sealed class PakfileLumpViewModel : BspNode, ILumpViewModel
{
    private readonly PakfileLump _pakfile;

    public SourceCache<PakfileEntryViewModel, string> Entries { get; } = new(entry => entry.Key);

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public PakfileLumpViewModel() => throw new NotImplementedException();

    public PakfileLumpViewModel(BspFile bsp)
    {
        BspService.Instance.ThrowIfNoLoadedBsp();

        _pakfile = bsp.GetLump<PakfileLump>();

        InitEntries();
    }
    public void UpdateEntries(bool checkIfModified)
    => Entries.Edit(updater =>
    {
        foreach (PakfileEntry entry in _pakfile.Entries.OrderBy(x => new FileInfo(x.Key).Name))
        {
            if (entry.IsModified || !checkIfModified)
                updater.AddOrUpdate(CreateEntryViewModel(entry));
        }
    });

    private void InitEntries() =>
        UpdateEntries(false);

    private PakfileEntryViewModel CreateEntryViewModel(PakfileEntry entry)
        => entry.Key.EndsWith(".vtf", StringComparison.OrdinalIgnoreCase)
            ? new PakfileEntryVtfViewModel(entry, this)
            : new PakfileEntryTextViewModel(entry, this);

    public PakfileEntryViewModel AddEntry(string key, Stream stream,
        ISourceUpdater<PakfileEntryViewModel, string>? updater = null)
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

    // Directories in Source that are sometimes omitted from a property, e.g. an
    // ambient_generic's "message" (sound file) doesn't need sound/ on the front,
    // for a sound file in sound/foo/bar, foo/bar/ is valid.
    private static readonly string[] SourceRootDirectories = [
        "materials",
        "scripts",
        "sound",
        "particles",
        "cfg",
        "models",
        "resource"
    ];

    // Lots of stuff in paklump isn't text and it's expensive to read and parse,
    // so we're quite conservative with what we check, could add more in future.
    private static readonly string[] RefactorablePakfileTypes = ["txt", "vmt", "cfg"];

    // Dict of file types for which may try to omit the RHS file extensions when
    // searching the LHS file type. For example consider this .vmt file:
    //
    // "LightmappedGeneric"
    // {
    //   "$basetexture" "foo/bar"
    // }
    //
    // Source will expects this file to be in materials/foo/bar.vtf.
    // SourceRootDirectories lets our search omit the materials/ part,
    // then IgnoreExtensions lets us omit the .vtf part.
    //
    // There's bound to be other file format combinations we could add here, I just
    // havent't spent much time looking. When adding new entries be *very* careful -
    // it make be that for some files, the cases where the extensions can be omitted
    // depend on specific KV1 keys.
    private static readonly Dictionary<string, string[]> IgnorableExtensions =
        new() {
            { ".vmt", [".vtf"] }
        };

    /// <summary>
    /// Update references to a path when a file moves, scanning the entity lump and
    /// text parts of pakfile lump. Could do texdata in future maybe?
    /// </summary>
    public void UpdatePathReferences(string newPath, string oldPath)
    {
        // Source does case-insensitive comparisons for filenames practically everywhere.
        // Probably because Windows filenames are treated case-insensitively.
        const StringComparison cmp = StringComparison.OrdinalIgnoreCase;

        // For testing this code I use bhop_lego2 which has sound files referenced in both
        // ambient_generics (entlump) and soundscapes (paklump)

        // Source sometimes let you omit the topmost directory of a file path, e.g. sound/foo/bar.mp3
        // can be used as just foo/bar.mp3. Quite a lot of faff to handle both cases, with and without prefix.
        var opSplit = oldPath.Split('/');
        var opPrefix = opSplit[0];
        var opNoPrefix = string.Join("/", opSplit[1..]);
        var directoryMatch = SourceRootDirectories.FirstOrDefault(s => s == opPrefix);

        // Entities
        foreach (EntityViewModel entity in BspService.Instance.EntityLumpViewModel?.Entities.Items ?? [])
        {
            foreach (EntityPropertyViewModel prop in entity.Properties)
            {
                if (prop is not EntityPropertyStringViewModel { Value: not null } sProp)
                    continue;

                var propValue = sProp.Value;
                if (!propValue.EndsWith(opNoPrefix, cmp))
                    // Definitely not a match
                    continue;

                var updatedOp = oldPath;
                var updatedNp = newPath;
                // Split this check from above for perf - vast majority of values are misses, move on ASAP.
                if (directoryMatch is not null && !propValue.StartsWith(directoryMatch, cmp))
                {
                    // This is case where something moves from sound/foo/bar.mp3 to materials/bar.mp3 and the matching
                    // property with foo/bar.mp3 - they are almost certainly going to break something.
                    if (!newPath.Split('/')[0].Equals(directoryMatch, cmp))
                    {
                        Logger.Warn(
                            $"Could move {prop.Key} property of {entity.PresentableName} from {oldPath} " +
                            $"to {newPath} but looks like it would create an invalid path!");
                        continue;
                    }

                    // Old path matched something but it had a recognised prefix on front: remove it
                    updatedOp = string.Join("/", oldPath.Split('/')[1..]);
                    updatedNp = string.Join("/", newPath.Split('/')[1..]);
                }
                else if (!propValue.Equals(oldPath, cmp))
                {
                    // Didn't match with basedirectory removed from front,
                    // and wasn't an exact match: not actually a match.
                    continue;
                }

                sProp.Value = updatedNp;
                sProp.MarkAsModified();
                Logger.Info($"Updated {prop.Key} property of {entity.PresentableName} from {updatedOp} to {updatedNp}");
            }
        }


        // Pakfiles. Lot of same logic as above, but horrible to combine.
        foreach (PakfileEntryTextViewModel entry in
                 Entries.Items
                     .OfType<PakfileEntryTextViewModel>()
                     .Where(item => RefactorablePakfileTypes
                         .Any(type => item.Key.EndsWith(type, cmp))))
        {
            if (!entry.IsContentLoaded)
                entry.LoadContent();

            if (entry.Content is null)
                return;

            var updatedOp = oldPath;
            var updatedNp = newPath;
            var matchIndex = -1;
            var changes = 0;

            var tryWithoutExtension = IgnorableExtensions.TryGetValue(Path.GetExtension(entry.Key), out var opNoExtension);
            while (true)
            {
                var match = entry.Content.IndexOf(opNoPrefix, startIndex: matchIndex + 1, cmp);

                var sliceFromEnd = 0;
                if (match == -1 && tryWithoutExtension)
                {
                    foreach (var ext in opNoExtension!)
                    {
                        if (!opNoPrefix.EndsWith(ext, cmp))
                            continue;

                        match = entry.Content.IndexOf(
                            opNoPrefix[..^ext.Length],
                            startIndex: matchIndex + 1,
                            comparisonType: cmp);

                        if (match != -1)
                        {
                            sliceFromEnd = ext.Length;
                            break;
                        }
                    }
                }

                if (match == -1)
                    break;

                matchIndex = match;

                if (directoryMatch is not null)
                {
                    updatedOp = string.Join("/", oldPath.Split('/')[1..])[..^sliceFromEnd];
                    updatedNp = string.Join("/", newPath.Split('/')[1..])[..^sliceFromEnd];
                    entry.Content = entry.Content[..matchIndex] + updatedNp +
                                    entry.Content[(matchIndex + updatedOp.Length)..];
                    changes++;
                    entry.IsModified = true;
                }
                else
                {
                    var wholeMatchIndex = matchIndex - opPrefix.Length - 1;

                    if (!entry.Content[wholeMatchIndex..(oldPath.Length - sliceFromEnd)]
                            .Equals(oldPath[..^sliceFromEnd], cmp))
                    {
                        continue;
                    }

                    updatedOp = oldPath;
                    updatedNp = newPath;
                    entry.Content =
                        entry.Content[..wholeMatchIndex] + updatedNp +
                        entry.Content[(wholeMatchIndex + updatedOp.Length)..];

                    changes++;
                    entry.IsModified = true;
                }
            }

            // Assumption that if a file is omitting the root dir prefix in some cases,
            // it's doing it in all cases, otherwise message would be a bit wrong.
            // It'd be extremely weird in one file was in some cases using both
            // sound/foo/bar.mp3 and foo/bar.mp3.
            if (changes > 0)
                Logger.Info($"Replaced {changes} instances of {updatedOp} with {updatedNp} in file {entry.Key}");
        }
    }

    public void Dispose()
    {
        Entries.Clear();
        Entries.Dispose();
    }
}
