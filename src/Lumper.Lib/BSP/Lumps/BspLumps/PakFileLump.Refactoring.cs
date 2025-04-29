namespace Lumper.Lib.Bsp.Lumps.BspLumps;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lumper.Lib.Bsp.Lumps.GameLumps;
using Lumper.Lib.Bsp.Struct;

public partial class PakfileLump
{
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
    // it may be that for some files, the cases where the extensions can be omitted
    // depend on specific KV1 keys.
    private static readonly Dictionary<string, string[]> IgnorableExtensions = new() { { ".vmt", [".vtf"] } };

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

    // Chars that are not valid in a path (maybe they are but really shouldn't be). Used to decide if we're at the end
    // of a string in a common Source file e.g. closing quotes in a VMT value.
    private static readonly char[] ControlChars = ['\n', '\r', '\t', ' ', '(', ')', '{', '}', '[', ']', '=', '"', '\''];

    // Far as we can tell, Source allows both / and \ as separators everywhere, and can even handle paths using
    // a combination of the two (fuck's sake).
    // So we need to split on both, and when we need to rejoin we always use /
    private static readonly char[] Separators = ['/', '\\'];

    // Source does case-insensitive comparisons for filenames practically everywhere.
    // Probably because Windows filenames are treated case-insensitively.
    private const StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

    // Can't use BspLumpType here, static prop is on the GameLumpType enum.
    public enum PathReferenceUpdateType
    {
        Entity,
        Pakfile,
        TexData,
        StaticProp,
    }

    /// <summary>
    /// Updates all instances of a given `oldPath` to `newPath`.
    /// If `UpdateTypes` is null, all lump types will be updated, otherwise only the specified types.
    /// <returns>List of all the types of lumps that were updated</returns>
    /// </summary>
    public List<PathReferenceUpdateType> UpdatePathReferences(
        string oldPath,
        string newPath,
        List<PathReferenceUpdateType>? updateTypes = null,
        string[]? limitPakfileExtensions = null
    )
    {
        limitPakfileExtensions ??= TextFileTypes;

        // Bail if either path doesn't contain a separator so slices can't result in 0-length arrays.
        // We'd be in the top-most directory anyway, nothing in Source loads files from there.
        if (!Separators.Any(oldPath.Contains) || !Separators.Any(newPath.Contains))
            return [];

        var updatedTypes = new List<PathReferenceUpdateType>();

        // Source sometimes let you omit the topmost directory of a file path, e.g. sound/foo/bar.mp3
        // can be used as just foo/bar.mp3. Quite a lot of faff to handle both cases, with and without prefix.
        // Var names get long here, using "op" for oldPath and "np" for newPath.
        string[] opSplit = oldPath.Split(Separators);

        string opPrefix = opSplit[0];
        string opNoPrefix = string.Join('/', opSplit[1..]);
        string? directoryMatch = SourceRootDirectories.FirstOrDefault(s => s == opPrefix);

        // This case is where something moves from sound/foo/bar.mp3 to materials/bar.mp3
        if (directoryMatch is not null && !newPath.StartsWith(directoryMatch, Comparison))
        {
            Logger.Warn(
                $"Refusing to refactor references of {oldPath} to {newPath}. "
                    + "Moving between base Source directories is almost certainly going to break something."
            );
            return [];
        }

        if (updateTypes is null || updateTypes.Contains(PathReferenceUpdateType.Entity))
        {
            if (UpdateEntityPathReferences(oldPath, newPath, opNoPrefix, directoryMatch))
                updatedTypes.Add(PathReferenceUpdateType.Entity);
        }

        if (updateTypes is null || updateTypes.Contains(PathReferenceUpdateType.Pakfile))
        {
            if (UpdatePakfilePathReferences(oldPath, newPath, limitPakfileExtensions, opNoPrefix, directoryMatch))
                updatedTypes.Add(PathReferenceUpdateType.Pakfile);
        }

        if (updateTypes is null || updateTypes.Contains(PathReferenceUpdateType.TexData))
        {
            if (UpdateTexDataPathReferences(oldPath, newPath, opPrefix, opNoPrefix))
                updatedTypes.Add(PathReferenceUpdateType.TexData);
        }


        return updatedTypes;
    }

    private bool UpdateEntityPathReferences(string oldPath, string newPath, string opNoPrefix, string? directoryMatch)
    {
        bool updated = false;

        foreach (Entity entity in Parent.GetLump<EntityLump>().Data)
        {
            foreach (Entity.EntityProperty prop in entity.Properties)
            {
                if (prop is not Entity.EntityProperty<string> { Value: not null } sProp)
                    continue;

                string propValue = sProp.Value;
                if (!propValue.EndsWith(opNoPrefix, Comparison))
                    // Definitely not a match
                    continue;

                string op = oldPath;
                string np = newPath;
                // Split this check from above for perf - vast majority of values are misses, move on ASAP.
                if (directoryMatch is not null && !propValue.StartsWith(directoryMatch, Comparison))
                {
                    // Old path matched something but it had a recognised prefix on front: remove it
                    op = op[(op.IndexOf('/') + 1)..];
                    np = np[(np.IndexOf('/') + 1)..];
                }
                else if (!propValue.Equals(oldPath, Comparison))
                {
                    // Didn't match with basedirectory removed from front,
                    // and wasn't an exact match: not actually a match.
                    continue;
                }

                sProp.Value = np;
                updated = true;
                Logger.Info($"Replaced {prop.Key} property of {op} to {np} for entity {entity.PresentableName}");
            }
        }

        return updated;
    }

    private bool UpdatePakfilePathReferences(
        string oldPath,
        string newPath,
        string[] limitExtensions,
        string opNoPrefix,
        string? directoryMatch
    )
    {
        bool updated = false;

        foreach (
            PakfileEntry entry in Entries.Where(item =>
                limitExtensions.Any(type => Path.GetExtension(item.Key).Equals(type, Comparison))
            )
        )
        {
            // Pretty big perf hit since entry.GetData() is a ZipArchive read, though we should only be reading text
            // files, so unlikely to be that large.
            string text = BspFile.Encoding.GetString(entry.GetData());
            if (string.IsNullOrWhiteSpace(text))
                continue;

            int changes = 0;

            // Base index that tracks progress iterating through entire file
            int baseIdx = -1;
            bool tryWithoutExtension = IgnorableExtensions.TryGetValue(
                Path.GetExtension(entry.Key),
                out string[]? extensions
            );

            // Outer loop as we traverse the entire file - could have multiple matches.
            while (true)
            {
                string op = oldPath;
                string np = newPath;

                // If we matched a directory, we're almost certainly right to omit it.
                if (directoryMatch is not null)
                {
                    op = op[(op.IndexOf('/') + 1)..];
                    np = np[(op.IndexOf('/') + 1)..];
                }

                // Index used to search forward from baseIdx
                // Start by searching with extension
                int searchIdx = text.IndexOf(op, startIndex: baseIdx + 1, Comparison);

                // If that match fails, try with extension omitted if appropriate file
                if (searchIdx == -1 && tryWithoutExtension)
                {
                    foreach (string ext in extensions!)
                    {
                        if (!Path.GetExtension(opNoPrefix).Equals(ext, Comparison))
                            continue;

                        searchIdx = text.IndexOf(opNoPrefix[..^ext.Length], startIndex: baseIdx + 1, Comparison);
                        if (searchIdx != -1)
                        {
                            op = op[..^ext.Length];
                            np = np[..^ext.Length];
                            break;
                        }
                    }
                }

                // No matches
                if (searchIdx == -1 || searchIdx >= text.Length - op.Length)
                    break;

                // Okay, we matched
                baseIdx = searchIdx;

                // Make sure we're not matching e.g. foo/bar.vtf with foo/barbaz.vtf
                char nextChar = text[baseIdx + op.Length];
                if (ControlChars.Contains(nextChar))
                {
                    text = text[..baseIdx] + np + text[(baseIdx + op.Length)..];
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
            {
                entry.UpdateData(BspFile.Encoding.GetBytes(text));
                entry.IsModified = true;
                updated = true;
                Logger.Info($"Replaced {changes} instances of {oldPath} with {newPath} in pakfile entry {entry.Key}");
            }
        }

        return updated;
    }

    private bool UpdateTexDataPathReferences(string oldPath, string newPath, string opPrefix, string opNoPrefix)
    {
        if (!Path.GetExtension(oldPath).Equals(".vmt", Comparison) || !opPrefix.Equals("materials", Comparison))
            return false;

        string op = opNoPrefix[..^4]; // Trim .vmt
        string np = newPath[(newPath.IndexOf('/') + 1)..^4]; // Trim materials/ and .vmt
        TexDataLump texdataLump =
            Parent.GetLump<TexDataLump>() ?? throw new InvalidDataException("TexDataLump not found");

        int count = 0;
        foreach (TexData texData in texdataLump.Data.Where(texData => texData.TexName.Equals(op, Comparison)))
        {
            texData.TexName = np;
            count++;
        }

        if (count > 0)
            Logger.Info($"Replaced {count} instances of {oldPath} to {newPath} in TexData lumps");

        return count > 0;
    }


    // As per https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/utils/vbsp/cubemap.cpp
    [GeneratedRegex(@"materials\/maps\/(.+)?/(?:(?:c-?\d+_-?\d+_-?\d+)|(?:cubemapdefault)(?:\.hdr)?\.vtf)")]
    private static partial Regex CubemapRegex();

    /// <summary>
    /// Renames the cubemap path as Source uses the filename when searching.
    /// Returns a dictionary with the key as the old string and the value as the new string.
    /// </summary>
    public List<(string oldPath, string newPath)> RenameCubemapPaths(string newFileName)
    {
        string baseFilename = Path.GetFileNameWithoutExtension(newFileName);
        var entriesModified = new List<(string, string)>();

        bool matched = false;
        foreach (PakfileEntry entry in Entries)
        {
            Match match = CubemapRegex().Match(entry.Key);
            if (match.Success)
            {
                matched = true;

                // Add the old key so we can update the UI later
                string oldString = entry.Key;

                string cubemapName = match.Groups[1].Value;
                entry.Rename(entry.Key.Replace(cubemapName, baseFilename));

                entriesModified.Add((oldString, entry.Key));
            }
        }

        if (matched)
        {
            IsModified = true;
            UpdateZip();
        }

        return entriesModified;
    }
}
