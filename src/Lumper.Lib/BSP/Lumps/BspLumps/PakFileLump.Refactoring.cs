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
    private static readonly Dictionary<string, string[]> IgnorableExtensions = new() { { ".vmt", [".vtf", ".vmt"] } };

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
            if (UpdateKv1PathReferences(oldPath, newPath, limitPakfileExtensions, opNoPrefix, directoryMatch))
                updatedTypes.Add(PathReferenceUpdateType.Pakfile);
        }

        if (updateTypes is null || updateTypes.Contains(PathReferenceUpdateType.TexData))
        {
            if (UpdateTexDataPathReferences(oldPath, newPath, opPrefix, opNoPrefix))
                updatedTypes.Add(PathReferenceUpdateType.TexData);
        }

        if (updateTypes is null || updateTypes.Contains(PathReferenceUpdateType.StaticProp))
        {
            if (UpdateStaticPropPathReferences(oldPath, newPath, opPrefix))
                updatedTypes.Add(PathReferenceUpdateType.StaticProp);
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

                string propValue = sProp.Value.Replace('\\', '/');
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

    private bool UpdateKv1PathReferences(
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

            string op = oldPath;
            string np = newPath;

            // If we matched a directory, we're almost certainly right to omit it.
            // UpdatePathReferences has already checked that the new path also starts with it.
            if (directoryMatch is not null)
            {
                op = op[(directoryMatch.Length + 1)..];
                np = np[(directoryMatch.Length + 1)..];
            }

            // Outer loop as we traverse the entire file - could have multiple matches.
            while (baseIdx < text.Length)
            {
                // Try to find a match, first with full path
                (int matchIdx, string matchedOp, string matchedNp) = FindPathMatch(
                    text,
                    baseIdx,
                    op,
                    np,
                    tryWithoutExtension,
                    extensions,
                    opNoPrefix
                );

                // No matches
                if (matchIdx == -1)
                    break;

                // We found a match at matchIdx
                baseIdx = matchIdx;

                // Check if it's a valid standalone path by examining surrounding characters
                if (IsValidPathMatch(text, baseIdx, matchedOp.Length))
                {
                    // Replace the match with the new path
                    text = text[..baseIdx] + matchedNp + text[(baseIdx + matchedOp.Length)..];
                    changes++;
                    entry.IsModified = true;

                    // Skip past what we just inserted
                    baseIdx += matchedNp.Length;
                }
                else
                {
                    // Not a valid match, move ahead one character
                    baseIdx += 1;
                }
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

    // Helper method to find a path match, handling both regular and extension-less cases
    private static (int matchIndex, string matchedOp, string matchedNp) FindPathMatch(
        string text,
        int startIdx,
        string op,
        string np,
        bool tryWithoutExtension,
        string[]? extensions,
        string opNoPrefix
    )
    {
        // First try matching with the full path
        int matchIndex = FindSlashAgnosticMatch(text, startIdx, op);

        if (matchIndex != -1)
            return (matchIndex, op, np);

        // If that match fails, try with extension omitted if appropriate file
        if (tryWithoutExtension)
        {
            foreach (string ext in extensions!)
            {
                if (!Path.GetExtension(opNoPrefix).Equals(ext, Comparison))
                    continue;

                string opWithoutExt = op[..^ext.Length];
                string npWithoutExt = np[..^ext.Length];

                matchIndex = FindSlashAgnosticMatch(text, startIdx, opWithoutExt);

                if (matchIndex != -1)
                    return (matchIndex, opWithoutExt, npWithoutExt);
            }
        }

        return (-1, string.Empty, string.Empty);
    }

    // Find a match that allows mixed forward/backslashes. Really gross to do, but we've genuinely
    // seen "$basetexture"	"_css/de_train\train_metalceiling_02" in the wild.
    private static int FindSlashAgnosticMatch(string text, int startIdx, string pattern)
    {
        if (startIdx < 0)
            startIdx = 0;

        for (int i = startIdx; i <= text.Length - pattern.Length; i++)
        {
            bool isMatch = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                char textChar = text[i + j];
                char patternChar = pattern[j];

                // If we're at a separator position in the pattern, match either separator
                if (patternChar is '/' or '\\')
                {
                    if (textChar is not '/' and not '\\')
                    {
                        isMatch = false;
                        break;
                    }
                }
                // Otherwise case-insensitive match
                else if (char.ToUpperInvariant(textChar) != char.ToUpperInvariant(patternChar))
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
                return i;
        }

        return -1;
    }

    // Check if the match is a standalone path by examining surrounding characters
    // Need to check a bunch of cases:
    // - we're not matching foo/bar.vtf with foo/barbaz.vtf
    // - we're not matching foo/bar.vtf with baz/foo/bar.vtf
    //
    // It'd be really nice if we could use a KV1 parser here, but we want to preserve comments and generally
    // leave files as unchanged as possible. ValveKeyValue unfortunately doesn't expose the reader we could
    // use to do this... Unit tests for this are a pretty good at least.
    //
    // We can tolerate some very unusual edge cases of a found value actually being a key, or a potentially
    // invalid KV1 file. If value is quoted properly, or has whitespace to left and
    // whitespace/newline/comment to right, we can assume it's a valid match.
    private static bool IsValidPathMatch(string text, int matchIdx, int matchLength)
    {
        // Ensure we don't go out of bounds
        if (matchIdx <= 0 || matchIdx + matchLength >= text.Length)
            return false;

        char prevChar = text[matchIdx - 1];
        char nextChar = text[matchIdx + matchLength];

        if (prevChar == '"')
            return nextChar == '"';

        if (IsWhitespace(prevChar))
            return IsWhitespace(nextChar) || nextChar is '\r' or '\n' or '/';

        return false;
    }

    // C isspace https://en.cppreference.com/w/c/string/byte/isspace
    private static bool IsWhitespace(char c) =>
        c switch
        {
            ' ' => true,
            '\t' => true,
            '\r' => true,
            '\n' => true,
            '\v' => true,
            '\f' => true,
            _ => false,
        };

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

    private bool UpdateStaticPropPathReferences(string oldPath, string newPath, string opPrefix)
    {
        if (!Path.GetExtension(oldPath).Equals(".mdl", Comparison) || !opPrefix.Equals("models", Comparison))
            return false;

        List<string>? pathList = Parent.GetLump<GameLump>().GetLump<Sprp>()?.StaticPropsDict?.Data;

        if (pathList is null)
            return false;

        int match = pathList.FindIndex(name => name.Equals(oldPath, Comparison));
        if (match == -1)
            return false;

        pathList[match] = newPath;
        Logger.Info($"Updated static prop path from {oldPath} to {pathList[match]}");
        return true;
    }

    // Matches stuff stored in materials/maps/<mapname>/, if we're renaming and have the option enabled we'll move
    // everything.
    [GeneratedRegex("^materials/maps/([^/]+)/.*$")]
    private static partial Regex MapFileMaterialsRegex();

    // Matches soundscapes stored in scripts/soundscapes_<mapname>.txt
    [GeneratedRegex(@"^scripts/soundscapes_([^/]+)\.(?:txt|vsc)$")] // Strata supports .vsc, dunno why!
    private static partial Regex MapFileSoundscapeRegex();

    // Matches soundscripts stored in maps/<mapname>_level_sounds.txt
    [GeneratedRegex(@"^maps/([^/]+)_level_sounds\.txt$")]
    private static partial Regex MapFileSoundscriptRegex();

    // Matches particle manifest stored in maps/<mapname>_particles.txt
    [GeneratedRegex(@"^maps/([^/]+)_particles\.txt")]
    private static partial Regex MapFileParticlesRegex();

    private static readonly Regex[] MapFileRegexes =
    [
        MapFileMaterialsRegex(),
        MapFileSoundscapeRegex(),
        MapFileSoundscriptRegex(),
        MapFileParticlesRegex(),
    ];

    public void ProcessMapRename(string oldFileName, string newFileName)
    {
        string newName = Path.GetFileNameWithoutExtension(newFileName);

        foreach (PakfileEntry entry in Entries)
        {
            foreach (Regex regex in MapFileRegexes)
            {
                Match match = regex.Match(entry.Key);
                if (!match.Success)
                    continue;

                string mapName = match.Groups[1].Value;
                if (!mapName.Equals(oldFileName, Comparison))
                    continue;

                string oldKey = entry.Key;
                string newKey = entry.Key.Replace(mapName, newName);
                entry.Rename(newKey);

                Logger.Info($"Renamed pakfile entry {oldKey} to {newKey}");
                IsModified = true;

                UpdatePathReferences(oldKey, newKey);
            }
        }
    }
}
