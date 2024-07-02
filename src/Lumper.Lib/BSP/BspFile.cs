namespace Lumper.Lib.BSP;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bsp.Enum;
using Enum;
using IO;
using Lumps;
using Lumps.BspLumps;
using Newtonsoft.Json;
using NLog;
using Struct;

public partial class BspFile
{
    public const int HeaderLumps = 64;

    public const int HeaderSize = 1036;

    public const int MaxLumps = 128;

    public string? Name { get; private set; }

    [JsonIgnore]
    public string? FilePath { get; private set; }

    public int Revision { get; set; }

    public int Version { get; set; }

    public Dictionary<BspLumpType, Lump<BspLumpType>> Lumps { get; set; } = [];

    [JsonIgnore]
    public FileStream? FileStream { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // The nullability of all sorts of parts of this class and its members is based on the assumption
    // that an actual BSP has been loaded.
    // We don't support or plan to support creating BSPs from scratch, so private ctor blocks
    // creating a BspFile instance without loading an actual BSP.
    private BspFile() { }

    public static BspFile? FromPath(IoHandler handler, string path)
        => new BspFile { Name = Path.GetFileNameWithoutExtension(path) }
            .Load(handler, path);

    public static BspFile? FromStream(IoHandler handler, Stream stream)
        => new BspFile()
            .Load(handler, stream);

    public BspFile? Load(IoHandler handler, string path)
    {
        Name = Path.GetFileNameWithoutExtension(path);
        FilePath = GetUnescapedFilePathString(path);
        FileStream = File.OpenRead(FilePath);

        using var reader = new BspFileReader(this, FileStream, handler);

        // UI/CLI should track whether this gets cancelled and dispose of this class, it's in a useless
        // half-loaded state and mustn't be used.
        if (!reader.Load())
        {
            Logger.Info("Loading cancelled");
            return null;
        }

        Logger.Info($"Loaded {Name}.bsp");
        return this;
    }

    public BspFile? Load(IoHandler handler, Stream stream)
    {
        using var reader = new BspFileReader(this, stream, handler);

        if (!reader.Load())
        {
            Logger.Info("Loading cancelled");
            return null;
        }

        Logger.Info("Loaded BSP from stream");
        return this;
    }

    /// <summary>
    /// Save a BSP out to file, handling backups and updating the underlying
    /// file stream to use the new file.
    /// </summary>
    /// <exception cref="ArgumentException">Bad input args</exception>
    /// <exception cref="FileLoadException">
    /// When save successful but failed to update active file stream to the new file.
    /// The UI/CLI should do a full load of that file in that case.
    /// </exception>
    public void SaveToFile(
        IoHandler handler,
        string? path,
        DesiredCompression compress,
        bool makeBackup = true)
    {
        if (path is null && FilePath is null)
            throw new ArgumentException("Not given a path to write to, and current BSP doesn't have a path");

        string outPath;
        string? backupPath = null;
        string? escapedPath;
        if (path is not null &&
            (escapedPath = GetUnescapedFilePathString(path)) != FilePath)
        {
            outPath = escapedPath;
        }
        else
        {
            outPath = FilePath!;

            // Path is null so probably overwriting a file. If so, make a backup.
            // This program isn't well-tested, don't want it mangling someones's BSP.
            // Only do if overwriting existing file i.e. string != null
            if (makeBackup && File.Exists(outPath))
            {
                backupPath = BspExtensionRegex().Replace(outPath, "") +
                             $"_lumperbackup{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.bsp";
                if (File.Exists(backupPath))
                {
                    // This should never happen since path depends on current
                    // millisecond but whatever
                    Logger.Error($"Backup path {backupPath} exists!");
                    return;
                }

                try
                {
                    if (FileStream is not null)
                    {
                        using FileStream backupStream = File.Open(backupPath, FileMode.Create);
                        FileStream.Seek(0, SeekOrigin.Begin);
                        FileStream.CopyTo(backupStream);
                    }
                    else
                    {
                        File.Copy(outPath, backupPath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to create backup file, not saving");
                    return;
                }

                Logger.Info($"Created backup file {backupPath}");
            }
        }

        Stream stream;
        var overwritingOpenFile = false;
        // Check if we're trying to write to the file we current have open.
        // If so, saving will try to use data from that open filestream, so we
        // can't write to it at the same time.
        // Instead, write to a memory stream, then on success we'll close to
        // open filestream, re-open it, and write the memory stream out to it.
        if (File.Exists(outPath) && outPath == FilePath && FileStream is not null)
        {
            overwritingOpenFile = true;
            stream = new MemoryStream();
        }
        else
        {
            // Overwrites file if exists
            stream = File.Open(outPath, FileMode.Create);
        }

        var writer = new BspFileWriter(this, stream, handler, compress);
        bool success;
        try
        {
            success = writer.Save();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to write BSP file to stream");
            success = false;
        }

        if (success)
        {
            if (overwritingOpenFile)
            {
                // Dispose of filestream we were just reading from
                FileStream!.Dispose();

                // Reopen, FileMode.Create overwrites
                using FileStream fstream = File.Open(outPath, FileMode.Create);
                stream.Seek(0, SeekOrigin.Begin);

                // Copy memorystream we just wrote to over to new filestream, dispose
                stream.CopyTo(fstream);
                // Dispose of writer and underlying `stream`.
                writer.Dispose();
            }
            else
            {
                // This is a filestream to a new file, just flush and close.
                writer.Dispose();
            }

            // Update paths and name, and reopen the file we just wrote to, not reading anything.
            // This means we keep an open stream to the file (stops other processes tampering), and
            // can use it for unmanaged lump/pakfile (in some cases) when next saving, without
            // actually loading anything into memory yet.
            Name = Path.GetFileNameWithoutExtension(outPath);
            FilePath = outPath;
            FileStream = File.OpenRead(outPath);

            Logger.Info($"Saved {outPath}");

            if (!UpdateActiveBspFileSource(writer))
                throw new FileLoadException();

            return;
        }

        // Save failed. Stream we were just writing to is useless, dispose.
        // Save operation doesn't alter this BSP or underlying filestream if open.
        writer.Dispose();

        if (backupPath is null)
            return;

        // If we made a backup but save failed, that backup is the same file
        // the currently loaded one.
        try
        {
            File.Delete(backupPath);
            Logger.Info("Loaded BSP left unchanged, deleted identical backup file.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to restore backup!");
        }
    }

    /// <summary>
    /// When we save out to a new file, we update the active file stream to point to that file.
    /// But saving usually updates a bunch of headers, compression state, etc., so lumps that
    /// care about the active file stream need to be updated.
    ///
    /// This is really ugly, and would be nice to just completely reload the active BSP,
    /// but that'd either require a reset of most of the UI, or hackily updating a bunch
    /// of viewmodel -> model references.
    /// </summary>
    private bool UpdateActiveBspFileSource(BspFileWriter writer)
    {
        if (FileStream is null)
            return false;

        foreach ((BspLumpType bspLumpType, BspLumpHeader bspHeader) in writer.LumpHeaders)
        {
            Lump lump = GetLump(bspLumpType);

            if (lump is not GameLump and not PakfileLump)
                lump.IsCompressed = bspHeader.FourCc > 0;

            if (lump is GameLump gl)
            {
                if (gl.LastWriter is null)
                {
                    Logger.Error("Can't find writer we just used, unable to update active BSP instance.");
                    return false;
                }

                foreach ((Lump glLump, LumpHeaderInfo? glLumpHeader) in gl.LastWriter.HeaderInfo)
                {
                    glLump.IsCompressed = glLumpHeader.Compressed;

                    if (glLump is not IFileBackedLump glfbLump)
                        continue;

                    glfbLump.DataStream = FileStream;
                    glfbLump.DataStreamLength = glLumpHeader.Length;
                    glfbLump.DataStreamOffset = glLumpHeader.Offset;

                    if (glLump is IUnmanagedLump uGlLump)
                        uGlLump.UncompressedLength = glLumpHeader.Compressed ? glLumpHeader.UncompressedLength : -1;
                }

                // It was a hack to expose this at all, but need to know these headers.
                // We could probably structure all of this better but just don't have time.
                gl.LastWriter = null;

                continue;
            }

            if (lump is not IFileBackedLump fbLump)
                continue;

            fbLump.DataStream = FileStream;
            fbLump.DataStreamLength = bspHeader.Length;
            fbLump.DataStreamOffset = bspHeader.Offset;

            if (lump is IUnmanagedLump uLump)
            {
                uLump.UncompressedLength = bspHeader.FourCc > 0 ? bspHeader.FourCc : -1;
            }
            else if (lump is PakfileLump paklump)
            {
                foreach (PakfileEntry entry in paklump.Entries)
                    entry.IsModified = false;
            }
        }

        return true;
    }

    public bool SaveToStream(IoHandler handler, Stream stream, DesiredCompression compress)
    {
        using var writer = new BspFileWriter(this, stream, handler, compress);
        try
        {
            return writer.Save();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to write BSP to stream");
            return false;
        }
    }

    public T GetLump<T>() where T : Lump<BspLumpType> => Lumps.Values.OfType<T>().First();

    public Lump<BspLumpType> GetLump(BspLumpType lumpType) => Lumps[lumpType];

    // I tried to refactor a bunch of code to use URIs everywhere but was a big hassle and Uri ctor apparently
    // doesn't handle escaped stuff well... just sticking with strings
    private static string GetUnescapedFilePathString(string path) => Uri.UnescapeDataString(Path.GetFullPath(path));

    public void JsonDump(IoHandler handler, bool sortLumps, bool sortProperties, bool ignoreOffset)
    {
        var dir = Path.GetDirectoryName(FilePath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(FilePath);
        var path = Path.Join(dir, name + ".json");
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);

        JsonDump(handler, fileStream, sortLumps, sortProperties, ignoreOffset);
        Logger.Info($"Dumped JSON to {path}");
    }

    public void JsonDump(IoHandler handler, Stream stream, bool sortLumps, bool sortProperties, bool ignoreOffset)
    {
        if (sortLumps)
        {
            Lumps = Lumps
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        using var bspStream = new MemoryStream();
        using var bspWriter = new BspFileWriter(this, bspStream, handler, DesiredCompression.Unchanged);
        bspWriter.Save();

        var jsonSerializerSettings = new JsonSerializerSettings {
            ContractResolver = new JsonContractResolver {
                SortProperties = sortProperties, IgnoreOffset = ignoreOffset
            },
            Formatting = Formatting.Indented
        };

        var serializer = JsonSerializer.Create(jsonSerializerSettings);
        using var sw = new StreamWriter(stream);
        using var writer = new JsonTextWriter(sw);
        serializer.Serialize(writer, new { Bsp = this, Writer = bspWriter });
    }

    [GeneratedRegex("\\.bsp$")]
    public static partial Regex BspExtensionRegex();
}
