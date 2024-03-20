namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;
using System;
using System.IO;
using Lumper.Lib.BSP.Struct;
using Lumper.UI.Models;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class PakFileEntryVtfViewModel(PakFileEntryBranchViewModel parent,
    PakFileEntry entry, string name) : PakFileEntryLeafViewModel(parent, entry, name)
{
    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"PakFileEntry{(string.IsNullOrWhiteSpace(Name) ? "" : $" ({Name})")}";

    private string _info = "";

    public string Info
    {
        get => _info;
        set => this.RaiseAndSetIfChanged(ref _info, value);
    }

    private bool _isModified;

    public override bool IsModified => _isModified;

    private Image<Rgba32>? _image;

    public Image<Rgba32>? Image
    {
        get => _image;
        private set => this.RaiseAndSetIfChanged(ref _image, value);
    }

    private uint _frame;

    public uint Frame
    {
        get => _frame;
        private set
        {
            this.RaiseAndSetIfChanged(ref _frame, value);
            UpdateImage();
        }
    }

    private uint _face;

    public uint Face
    {
        get => _face;
        private set
        {
            this.RaiseAndSetIfChanged(ref _face, value);
            UpdateImage();
        }
    }

    private uint _slice;

    public uint Slice
    {
        get => _slice;
        private set
        {
            this.RaiseAndSetIfChanged(ref _slice, value);
            UpdateImage();
        }
    }

    private uint _mipmapLevel;

    public uint MipmapLevel
    {
        get => _mipmapLevel;
        private set
        {
            this.RaiseAndSetIfChanged(ref _mipmapLevel, value);
            UpdateImage();
        }
    }

    public uint FrameMax => _vtfData?.FrameCount - 1 ?? 0;
    public uint FaceMax => _vtfData?.FaceCount - 1 ?? 0;
    public uint MipmapMax => _vtfData?.MipmapCount - 1 ?? 0;

    private VtfFileData? _vtfData;

    public override void Open()
    {
        _vtfData = new VtfFileData(_entry);

        this.RaisePropertyChanged(nameof(FrameMax));
        this.RaisePropertyChanged(nameof(FaceMax));
        this.RaisePropertyChanged(nameof(MipmapMax));

        Info = $"MajorVersion: {_vtfData.MajorVersion}\n" +
               $"MinorVersion: {_vtfData.MinorVersion}\n" +
               $"Size: {_vtfData.ImageSize}\n" +
               $"Width: {_vtfData.ImageWidth}\n" +
               $"Height: {_vtfData.ImageHeight}\n" +
               $"Format: {Enum.GetName(_vtfData.ImageFormat)}\n" +
               $"Depth: {_vtfData.Depth}\n" +
               $"FrameCount: {_vtfData.FrameCount}\n" +
               $"FaceCount: {_vtfData.FaceCount}\n" +
               $"MipmapCount: {_vtfData.MipmapCount}\n" +
               $"Flags: {_vtfData.Flags.ToString().Replace(",", "\n")}\n";

        UpdateImage();
    }

    private void UpdateImage() => Image = _vtfData?.GetImage(Frame, Face, Slice, MipmapLevel);

    public static Image<Rgba32> ImageFromFileStream(Stream fileSteam) => SixLabors.ImageSharp.Image.Load<Rgba32>(fileSteam);

    public void SetImageData(Image<Rgba32> image)
    {
        if (_vtfData == null)
        {
            return;
        }

        _isModified = true;
        _vtfData.SetImageData(image, Frame, Face, Slice, MipmapLevel);
        UpdateImage();
    }

    public void SetNewImage(Image<Rgba32> image)
    {
        if (_vtfData == null)
        {
            return;
        }

        _isModified = true;
        _vtfData.SetNewImage(image);
        UpdateImage();
    }

    public override void Update()
    {
        _isModified = false;
        base.Update();
    }
}
