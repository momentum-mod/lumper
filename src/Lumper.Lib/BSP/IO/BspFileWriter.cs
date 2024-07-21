namespace Lumper.Lib.BSP.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bsp.Enum;
using Enum;
using Lumps;
using Lumps.BspLumps;
using Newtonsoft.Json;
using NLog;
using Struct;

public sealed class BspFileWriter(BspFile file, Stream output, IoHandler handler, DesiredCompression compression)
    : LumpWriter(output)
{
    [JsonIgnore]
    private readonly BspFile _bsp = file;

    [JsonProperty]
    public Dictionary<BspLumpType, BspLumpHeader> LumpHeaders { get; set; } = [];

    [JsonIgnore]
    protected override IoHandler Handler { get; set; } = handler;

    [JsonIgnore]
    protected override DesiredCompression Compression { get; set; } = compression;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public bool Save()
    {
        ConstructTexDataLumps();

        if (Handler.Cancelled)
            return false;
        Handler.UpdateProgress(0, "Writing lumps");
        WriteAllLumps();

        if (Handler.Cancelled)
            return false;
        Handler.UpdateProgress((float)IoHandler.WriteProgressProportions.Header, "Generating header");
        WriteHeader();

        return true;
    }

    private void WriteHeader()
    {
        Seek(0, SeekOrigin.Begin);

        Write(Encoding.ASCII.GetBytes("VBSP"));
        Write(_bsp.Version);

        foreach (BspLumpHeader? lump in LumpHeaders.OrderBy(x => x.Key).Select(x => x.Value))
        {
            Write(lump.Offset);
            Write(lump.Length);
            Write(lump.Version);
            Write(lump.FourCc);
        }

        Write(_bsp.Revision);
    }

    private void WriteAllLumps()
    {
        // Seek past the header
        BaseStream.Seek(BspFile.HeaderSize, SeekOrigin.Begin);

        const float incr = (float)IoHandler.WriteProgressProportions.OtherLumps / BspFile.HeaderLumps;

        foreach (BspLumpType lumpType in _bsp.Lumps.Select(x => x.Key))
        {
            if (Handler.Cancelled)
                return;

            Handler.UpdateProgress(incr, $"Writing {lumpType}");

            Lump<BspLumpType> lump = _bsp.Lumps[lumpType];
            if (!lump.Empty)
            {
                // Lump offsets (and their corresponding data lumps) are always rounded
                // up to the nearest 4-byte boundary, though the lump length may not be.
                var padTo = lumpType == BspLumpType.Physlevel ? 16 : 4;
                var pad = new byte[padTo - (BaseStream.Position % padTo)];
                if (pad.Length != padTo)
                    Write(pad);
            }

            LumpHeaderInfo newHeaderInfo = Write(lump);
            LumpHeaders[lumpType] = new BspLumpHeader(newHeaderInfo, lump.Version);

            Logger.Debug($"Wrote {lumpType} ({(int)lumpType})".PadRight(48)
                         + $"offset: {newHeaderInfo.Offset}".PadRight(24)
                         + $"length: {newHeaderInfo.Length}");
        }
    }

    private void ConstructTexDataLumps()
    {
        List<TexData> texData = _bsp.GetLump<TexDataLump>().Data;
        TexDataStringDataLump texDataStringDataLump = _bsp.GetLump<TexDataStringDataLump>();

        // Ensure stringdata lump can fit everything we're about to stuff in
        texDataStringDataLump.Resize(texData.Sum(x => Encoding.ASCII.GetByteCount(x.TexName) + 1));

        List<int> stringTable = [];
        var pos = 0;
        foreach (TexData tex in texData)
        {
            // At start of texture string, put its loc in stringtable
            stringTable.Add(pos);

            tex.StringTablePointer = stringTable.Count - 1;

            var bytes = Encoding.ASCII.GetBytes(tex.TexName);
            Array.Copy(bytes, 0, texDataStringDataLump.Data, pos, bytes.Length);
            pos += bytes.Length;
            texDataStringDataLump.Data[pos] = 0;
            pos++;
        }

        _bsp.GetLump<TexDataStringTableLump>().Data = stringTable;
    }
}
