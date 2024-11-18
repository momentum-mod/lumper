namespace Lumper.Lib.BSP.Lumps.BspLumps;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bsp.Enum;
using Enum;
using IO;
using Lumps;
using Newtonsoft.Json;
using NLog;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;
using Struct;

/// <summary>
/// The pakfile lump (aka paklump) is just a zip archive for storing assets in the BSP.
/// The overall archive is always uncompressed, but each item may be LZMA compressed.
/// </summary>
public partial class PakfileLump(BspFile parent) : ManagedLump<BspLumpType>(parent), IFileBackedLump
{
    public List<PakfileEntry> Entries { get; private set; } = [];

    [JsonIgnore]
    public Stream DataStream { get; set; } = null!;

    public long DataStreamOffset { get; set; }

    public long DataStreamLength { get; set; }

    public bool IsModified { get; set; }

    [JsonIgnore]
    public ZipArchive Zip { get; private set; } = null!;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // As per https://github.com/ValveSoftware/source-sdk-2013/blob/master/mp/src/utils/vbsp/cubemap.cpp
    [GeneratedRegex(@"materials\/maps\/(.+)?/(?:(?:c-?\d+_-?\d+_-?\d+)|(?:cubemapdefault)(?:\.hdr)?\.vtf)")]
    private static partial Regex CubemapRegex();

    /// <summary>
    /// Updates all path references in the PakFileLump from oldPath to newPath,
    /// i.e. if a VMT references a VTF, it will be updated.
    /// </summary>
    public void UpdatePathReferences(string newPath, string oldPath, string? limitExtension = null)
    {
        var opSplit = oldPath.Split('/');
        var npSplit = newPath.Split('/');

        // VMTs can reference VTFs ignoring the root directory and without the extension
        oldPath = string.Join('/', opSplit[1..]);
        newPath = string.Join('/', npSplit[1..]);
        oldPath = Path.ChangeExtension(oldPath, "").TrimEnd('.');
        newPath = Path.ChangeExtension(newPath, "").TrimEnd('.');

        foreach (PakfileEntry entry in Entries)
        {
            if (limitExtension is null || !entry.Key.EndsWith(limitExtension))
                continue;

            var entryString = Encoding.Default.GetString(entry.GetReadOnlyStream().ToArray());
            var newString = entryString.Replace(oldPath, newPath, StringComparison.OrdinalIgnoreCase);

            if (newString != entryString)
                entry.UpdateData(Encoding.Default.GetBytes(newString));
        }
    }
    /// <summary>
    /// Gets the default cubemap path as Source uses the filename when searching.
    /// Returns a dictionary with the key as the old string
    /// and the value as the new string
    /// </summary>
    public Dictionary<string, string> GetCubemapsToChange(string newFileName)
    {
        string baseFilename = Path.GetFileNameWithoutExtension(newFileName);
        var entriesModified = new Dictionary<string, string>();

        foreach (PakfileEntry entry in Entries)
        {
            Match match = CubemapRegex().Match(entry.Key);
            if (match.Success)
            {
                var cubemapName = match.Groups[1].Value;
                entriesModified.Add(entry.Key, entry.Key.Replace(cubemapName, baseFilename));
            }
        }

        return entriesModified;
    }

    /// <summary>
    /// Renames the cubemap path as Source uses the filename when searching.
    /// Returns a dictionary with the key as the old string
    /// and the value as the new string
    /// </summary>
    public Dictionary<string,string> RenameCubemapsPath(string newFileName)
    {
        string baseFilename = Path.GetFileNameWithoutExtension(newFileName);
        var entriesModified = new Dictionary<string, string>();

        bool matched = false;
        foreach (PakfileEntry entry in Entries)
        {
            Match match = CubemapRegex().Match(entry.Key);
            if (match.Success)
            {
                matched = true;

                // Add the old key so we can update the UI later
                var oldString = entry.Key;

                var cubemapName = match.Groups[1].Value;
                entry.Key = entry.Key.Replace(cubemapName, baseFilename);
                entry.IsModified = true;

                entriesModified.Add(oldString, entry.Key);
            }
        }

        if (matched)
        {
            IsModified = true;
            UpdateZip();
        }

        return entriesModified;
    }

    public override void Read(BinaryReader reader, long length, IoHandler? handler = null)
    {
        DataStream = reader.BaseStream;
        DataStreamOffset = reader.BaseStream.Position;
        DataStreamLength = length;

        Stream stream = reader.BaseStream;
        var dataStream = new MemoryStream();
        int read;
        const int bufferSize = 80 * 1024;
        var incr = (float)bufferSize / length * (int)IoHandler.ReadProgressProportions.Paklump;
        var buffer = new byte[bufferSize];

        handler?.UpdateProgress(0, "Reading pakfile");
        var remaining = (int)length;
        while ((read = stream.Read(buffer, 0, int.Min(bufferSize, remaining))) > 0)
        {
            if (handler?.Cancelled ?? false)
                return;

            dataStream.Write(buffer, 0, read);
            remaining -= read;
            handler?.UpdateProgress(incr);
        }

        dataStream.Seek(0, SeekOrigin.Begin);
        Zip = ZipArchive.Open(dataStream);
        Entries = Zip.Entries.Select(entry => new PakfileEntry(this, entry)).ToList();

        // Mark the zip as compressed if we have any LZMA-compressed entries. We won't update an existing
        // entry when the lump has ShouldCompress: DesiredCompression.DontChange, but if a new item is added
        // we'll use this value.
        IsCompressed = Zip.Entries.Any(e => e is { CompressionType: CompressionType.LZMA });
    }

    public override void Write(Stream stream, IoHandler? handler = null, DesiredCompression? compression = null)
    {
        // If we have open filestream, pakfile isn't modified, and compression isn't
        // changing (or already already compressed), we can just read straight from filestream and write out again.
        if (!IsModified &&
            (compression == DesiredCompression.Unchanged ||
             IsCompressed ||
             (!IsCompressed && compression == DesiredCompression.Uncompressed)))
        {
            if (IsCompressed && compression == DesiredCompression.Uncompressed)
            {
                Logger.Debug(
                    "Saving uncompressed but the pakfile lump is unmodified and already compressed, leaving as-is.");
            }

            DataStream.Seek(DataStreamOffset, SeekOrigin.Begin);

            var buffer = ArrayPool<byte>.Shared.Rent(80 * 1024);
            try
            {
                int read;
                var remaining = DataStreamLength;
                while ((read = DataStream.Read(buffer, 0, int.Min(buffer.Length, (int)remaining))) > 0)
                {
                    stream.Write(buffer, 0, read);
                    remaining -= read;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        else if (compression == DesiredCompression.Uncompressed)
        {
            // MUCH faster path, since we can use the existing SharpCompress zip
            if (handler?.Cancelled ?? false)
                return;

            const float prog = (float)IoHandler.WriteProgressProportions.Paklump;
            handler?.UpdateProgress(0.25f * prog, "Updating pakfile zip");

            UpdateZip();

            if (handler?.Cancelled ?? false)
                return;

            handler?.UpdateProgress(0.75f * prog, "Writing pakfile contents");
            using var dataStream = new MemoryStream();
            Zip.SaveTo(dataStream, new WriterOptions(CompressionType.None));
            dataStream.Seek(0, SeekOrigin.Begin);
            dataStream.CopyTo(stream);

            IsCompressed = false;
        }
        else
        {
            // BASTARD slow path, have to reconstruct the zip every time. Might be possible to avoid but having
            // fought with SharpCompress for hours I can't figure it out.
            using var outStream = new MemoryStream();
            var zipWriter =
                (ZipWriter)WriterFactory.Open(outStream,
                    ArchiveType.Zip,
                    new ZipWriterOptions(CompressionType.None));

            var numEntries = Entries.Count;
            var incr = (float)IoHandler.WriteProgressProportions.Paklump / numEntries;
            // No need to update zip, we're reconstructing from scratch.
            foreach ((PakfileEntry entry, var index) in Entries.Select((x, i) => (x, i)))
            {
                if (handler?.Cancelled ?? false)
                    return;

                handler?.UpdateProgress(incr, $"Packing {entry.Key} ({index + 1}/{numEntries})");

                using Stream zipStream = zipWriter.WriteToStream(entry.Key,
                    new ZipWriterEntryOptions {
                        CompressionType = CompressionType.LZMA,
                        ModificationDateTime = entry.ZipEntry?.LastModifiedTime
                    });

                entry.GetReadOnlyStream().CopyTo(zipStream);
            }

            zipWriter.Dispose();

            outStream.Seek(0, SeekOrigin.Begin);
            outStream.CopyTo(stream);

            IsCompressed = true;
        }
    }

    /// <summary>
    /// Update the contents of the zip based on what's changed on this class.
    ///
    /// This method is only helpful for when saving uncompressed, and gives a decent perf
    /// boost in that case.
    ///
    /// When saving compressed, I can't find a way to avoid reconstructed the zip from scratch
    /// using a ZipWriter, .
    /// </summary>
    private void UpdateZip()
    {
        // Delete every item from the zip that's not in the PakLump entries (was completely deleted by user)
        foreach (ZipArchiveEntry entry in Zip.Entries
                     .Where(entry => Entries.All(x => x.ZipEntry != entry))
                     .ToList())
        {
            Zip.RemoveEntry(entry);
        }

        foreach (PakfileEntry entry in Entries)
        {
            if (entry.IsModified)
            {
                // Delete existing entry if exists
                ZipArchiveEntry? existingEntry = Zip.Entries.FirstOrDefault(e => e == entry.ZipEntry);
                if (existingEntry is not null)
                    Zip.RemoveEntry(existingEntry);

                // Add new/updated entry. Compressed if we're compressing everything, or Pakfile contains at least one
                // compressed entry (so map is for an engine build that supports LZMA)
                Zip.AddEntry(entry.Key, entry.GetReadOnlyStream(), true);
            }
            // New items that haven't been marked IsModified (they probably should've been, whatever)
            else if (entry.ZipEntry is null)
            {
                Zip.AddEntry(entry.Key, entry.GetReadOnlyStream(), true);
            }
        }
    }

    public override bool Empty => Entries.Count == 0;
}
