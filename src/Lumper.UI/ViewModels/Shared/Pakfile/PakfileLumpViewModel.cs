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
using NLog;

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
        entry.Key.EndsWith(".vtf", StringComparison.OrdinalIgnoreCase)
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

    // Directories in Source that are sometimes omitted from a property, e.g. an
    // ambient_generic's "message" (sound file) doesn't need sound/ on the front,
    // for a sound file in sound/foo/bar, foo/bar/ is valid.
    private static readonly string[] SourceRootDirectories =
    [
        "materials",
        "scripts",
        "sound",
        "particles",
        "cfg",
        "models",
        "resource",
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
    private static readonly Dictionary<string, string[]> IgnorableExtensions = new() { { ".vmt", [".vtf"] } };

    // Chars that are not valid in a path (maybe they are but really shouldn't be). Used to decide if we're at the end
    // of a string in a common Source file e.g. closing quotes in a VMT value.
    private static readonly char[] ControlChars = ['\n', '\r', '\t', ' ', '(', ')', '{', '}', '[', ']', '=', '"', '\''];

    /// <summary>
    /// Update references to a path when a file moves, scanning the entity lump, text parts of the
    /// pakfile lump, and texdata.
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
        string[] opSplit = oldPath.Split('/');
        string opPrefix = opSplit[0];
        string opNoPrefix = string.Join("/", opSplit[1..]);
        string? directoryMatch = SourceRootDirectories.FirstOrDefault(s => s == opPrefix);

        // Entities
        foreach (EntityViewModel entity in BspService.Instance.EntityLumpViewModel?.Entities.Items ?? [])
        {
            foreach (EntityPropertyViewModel prop in entity.Properties)
            {
                if (prop is not EntityPropertyStringViewModel { Value: not null } sProp)
                    continue;

                string propValue = sProp.Value;
                if (!propValue.EndsWith(opNoPrefix, cmp))
                    // Definitely not a match
                    continue;

                string op = oldPath;
                string np = newPath;
                // Split this check from above for perf - vast majority of values are misses, move on ASAP.
                if (directoryMatch is not null && !propValue.StartsWith(directoryMatch, cmp))
                {
                    // This is case where something moves from sound/foo/bar.mp3 to materials/bar.mp3 and the matching
                    // property with foo/bar.mp3 - they are almost certainly going to break something.
                    if (!newPath.Split('/')[0].Equals(directoryMatch, cmp))
                    {
                        Logger.Warn(
                            $"Could move {prop.Key} property of {entity.PresentableName} from {oldPath} "
                                + $"to {newPath} but looks like it would create an invalid path!"
                        );
                        continue;
                    }

                    // Old path matched something but it had a recognised prefix on front: remove it
                    op = string.Join("/", oldPath.Split('/')[1..]);
                    np = string.Join("/", newPath.Split('/')[1..]);
                }
                else if (!propValue.Equals(oldPath, cmp))
                {
                    // Didn't match with basedirectory removed from front,
                    // and wasn't an exact match: not actually a match.
                    continue;
                }

                sProp.Value = np;
                sProp.MarkAsModified();
                Logger.Info($"Updated {prop.Key} property of {entity.PresentableName} from {op} to {np}");
            }
        }

        // Pakfiles. Lot of same logic as above, but horrible to combine.
        foreach (
            PakfileEntryTextViewModel entry in Entries
                .Items.OfType<PakfileEntryTextViewModel>()
                .Where(item => RefactorablePakfileTypes.Any(type => item.Key.EndsWith(type, cmp)))
        )
        {
            if (!entry.IsContentLoaded)
                entry.LoadContent();

            if (entry.Content is null)
                return;

            int changes = 0;

            // Base index that tracks progress iterating through entire file
            int baseIdx = -1;
            bool tryWithoutExtension = IgnorableExtensions.TryGetValue(
                Path.GetExtension(entry.Key),
                out string[]? opNoExtension
            );

            // Outer loop as we traverse the entire file - could have multiple matches.
            while (true)
            {
                string op = oldPath;
                string np = newPath;

                // If we matched a directory, we're almost certainly right to omit it unless the pakfile is completely
                // fucked.
                if (directoryMatch is not null)
                {
                    op = string.Join("/", op.Split('/')[1..]);
                    np = string.Join("/", np.Split('/')[1..]);
                }

                // Index used to search forward from baseIdx
                // Start by searching with extension
                int searchIdx = entry.Content.IndexOf(op, startIndex: baseIdx + 1, cmp);

                // If that match fails, try with extension omitted if appropriate file
                if (searchIdx == -1 && tryWithoutExtension)
                {
                    foreach (string ext in opNoExtension!)
                    {
                        if (!opNoPrefix.EndsWith(ext, cmp))
                            continue;

                        searchIdx = entry.Content.IndexOf(opNoPrefix[..^ext.Length], startIndex: baseIdx + 1, cmp);
                        if (searchIdx != -1)
                        {
                            op = op[..^ext.Length];
                            np = np[..^ext.Length];
                            break;
                        }
                    }
                }

                // No matches
                if (searchIdx == -1 || searchIdx >= entry.Content.Length - op.Length)
                    break;

                // Okay, we matched
                baseIdx = searchIdx;

                // Make sure we're not matching e.g. foo/bar.vtf with foo/barbaz.vtf
                char nextChar = entry.Content[baseIdx + op.Length];
                if (ControlChars.Contains(nextChar))
                {
                    entry.Content = entry.Content[..baseIdx] + np + entry.Content[(baseIdx + op.Length)..];
                    changes++;
                    entry.IsModified = true;
                }

                baseIdx += np.Length; // Move to end of new path
            }

            // Assumption that if a file is omitting the root dir prefix in some cases,
            // it's doing it in all cases, otherwise message would be a bit wrong.
            // It'd be extremely weird in one file was in some cases using both
            // sound/foo/bar.mp3 and foo/bar.mp3.
            if (changes > 0)
                Logger.Info($"Replaced {changes} instances of {oldPath} with {newPath} in file {entry.Key}");
        }

        // TexData - doesn't have viewmodels (yay!)
        if (oldPath.EndsWith(".vmt", cmp) && opPrefix.Equals("materials", cmp))
        {
            string op = opNoPrefix[..^4]; // Trim .vtf
            string np = string.Join('/', newPath.Split('/')[1..])[..^4]; // Trim materials/ and .vtf
            TexDataLump? texdataLump = BspService.Instance.BspFile?.GetLump<TexDataLump>();
            if (texdataLump is null)
                throw new InvalidDataException("TexDataLump not found (??)");

            foreach (TexData texData in texdataLump.Data)
            {
                if (texData.TexName.Equals(op, cmp))
                    texData.TexName = np;
            }
        }
    }

    public void Dispose()
    {
        Entries.Clear();
        Entries.Dispose();
    }
}
