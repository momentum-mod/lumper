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

public sealed partial class BspFile : IDisposable
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
        FileStream?.Dispose();
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

    public void SaveToFile(
        IoHandler handler,
        string? path,
        DesiredCompression compress,
        bool makeBackup)
    {
        // If you add something here, also add it to the BspReader
        if (path is null && FilePath is null)
            throw new ArgumentException("Not given a path to write to, and current BSP doesn't have a path");

        string outPath;
        string? backupPath = null;

        var escapedPath = path is null ? null : GetUnescapedFilePathString(path);
        if (escapedPath is null || escapedPath == FilePath)
        {
            outPath = FilePath!;

            // Path is null or matches current file, so we're overwriting a file.
            // If so, make a backup if requested.
            if (makeBackup && File.Exists(outPath))
            {
                backupPath = BspExtensionRegex().Replace(outPath, "") +
                             $"_lumperbackup{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.bsp";
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
        else
        {
            outPath = escapedPath;
        }

        // This stream needs to be closed at precise times so no using/try-finally; be careful
        // to dispose of it in all code paths.
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

        // This writer is configured to *not* dispose of the underlying stream on dispose - in
        // fact we dispose of the stream ourselves earlier. But using is fine for handling
        // disposing of the writer class itself.
        using var writer = new BspFileWriter(this, stream, handler, compress);
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

                // Copy memorystream we just wrote to over to new filestream
                stream.CopyTo(fstream);
            }

            // Flush and dispose. We no longer need this stream, and we may be opening new
            // stream to the same file. Even though the new stream is read-only, easiest to
            // just dispose of this one ASAP.
            stream.Dispose();

            // Update paths and name, and reopen the file we just wrote to, not reading anything.
            // This means we keep an open stream to the file (stops other processes tampering), and
            // can use it for unmanaged lump/pakfile (in some cases) when next saving, without
            // actually loading anything into memory yet.
            Name = Path.GetFileNameWithoutExtension(outPath);
            FilePath = outPath;
            FileStream = File.OpenRead(outPath);

            Logger.Info($"Saved {outPath}");
        }
        else
        {
            // Save failed. Stream we were just writing to is useless, dispose.
            // Save operation doesn't alter this BSP or underlying filestream if open.
            stream.Dispose();

            if (backupPath is not null)
            {
                // If we made a backup but save failed, that backup is the same file
                // the currently loaded one.
                try
                {
                    File.Delete(outPath);
                    Logger.Info("Loaded BSP left unchanged, deleted identical backup file.");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to restore backup!");
                }
            }
        }
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
            ContractResolver = new JsonContractResolver(sortProperties, ignoreOffset),
            Formatting = Formatting.Indented
        };

        var serializer = JsonSerializer.Create(jsonSerializerSettings);
        using var sw = new StreamWriter(stream);
        using var writer = new JsonTextWriter(sw);
        serializer.Serialize(writer, new { Bsp = this, Writer = bspWriter });
    }

    [GeneratedRegex("\\.bsp$")]
    public static partial Regex BspExtensionRegex();

    public void Dispose() => FileStream?.Dispose();
}
