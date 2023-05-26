using System;
using System.IO;
using System.Linq;
//using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DynamicData;
using SharpCompress.Archives.Zip;
using ReactiveUI;
using VTFLib;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

/// <summary>
///     ViewModel for <see cref="Lib.BSP.Struct.PakFileEntry" />.
/// </summary>
public class PakFileEntryViewModel : BspNodeBase
{
    public readonly SourceList<PakFileEntryViewModel> _entries = new();
    private readonly string _name;
    private readonly ZipArchiveEntry? _entry;
    private readonly PakFileLumpViewModel _pakFileLumpViewModel;

    public PakFileEntryViewModel(PakFileLumpViewModel parent, ZipArchive zip)
    : base(parent)
    {
        _pakFileLumpViewModel = parent;
        foreach (var entry in zip.Entries)
        {
            bla(entry);
        }
        InitializeNodeChildrenObserver();
    }

    //TODO constructor for file/leaf .. split entry view model dir <-> file
    public PakFileEntryViewModel(PakFileEntryViewModel parent,
        ZipArchiveEntry entry, string name)
        : base(parent)
    {
        _name = name;
        _entry = entry;
    }

    //TODO constructor for directory .. split entry view model dir <-> file
    public PakFileEntryViewModel(PakFileEntryViewModel parent,
        ZipArchiveEntry entry, string name, int index)
        : base(parent._pakFileLumpViewModel)
    {
        _pakFileLumpViewModel = parent._pakFileLumpViewModel;
        _entry = null;
        _name = name;
    }

    //todo rename and move to abstract class
    void bla(ZipArchiveEntry entry, int index = 0)
    {
        var path = entry.Key.Split('/');
        bool isDir;
        if (index == path.Length - 1)
            isDir = false;
        else
            isDir = true;
        string name = path[index];
        if (isDir)
        {
            //todo !@#&^@(!#^)R(*&%#!^(#@*P@&!$))
            string AAAAAAAA = name;
            //todo !@#&^@(!#^)R(*&%#!^(#@*P@&!$))
            var dir = _entries.AsObservableList().Items.FirstOrDefault(x => x._name == AAAAAAAA, null);
            if (dir is null)
            {
                dir = new PakFileEntryViewModel(this, entry, name, index);
                _entries.Add(dir);
            }
            dir.bla(entry, index + 1);
        }
        else
        {
            _entries.Add(new PakFileEntryViewModel(this, entry, name));
        }
    }
    private void InitializeNodeChildrenObserver()
    {
        InitializeNodeChildrenObserver(_entries);
        foreach (var entry in _entries.AsObservableList().Items)
        {
            entry.InitializeNodeChildrenObserver();
        }
    }

    public override BspNodeBase? ViewNode => this;

    public override string NodeName =>
        $"PakFileEntry{(string.IsNullOrWhiteSpace(_name) ? "" : $" ({_name})")}";

    public Stream? Stream { get; private set; }
    private string _content = "";
    public string Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    public Image? _image = null;
    public Image? Image
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }
    public override bool IsModified =>
        Nodes is { Count: > 0 } && Nodes.Any(n => n.IsModified);


    public void Open()
    {
        Stream = _entry.OpenEntryStream();
        if (_name.ToLower().EndsWith(".vtf"))
        {
            VTFAPI.Initialize();
            string fileName = "tmp.vtf";
            using var file = File.Open(fileName, FileMode.Create);
            Stream.CopyTo(file);

            uint image = 0;
            VTFFile.CreateImage(ref image);
            VTFFile.BindImage(image);
            VTFFile.ImageLoad(fileName, false);

            Content = $"MajorVersion: {VTFFile.ImageGetMajorVersion()}\n" +
                      $"MinorVersion: {VTFFile.ImageGetMinorVersion()}\n" +
                      $"Size: {VTFFile.ImageGetSize()}\n" +
                      $"Width: {VTFFile.ImageGetWidth()}\n" +
                      $"Height: {VTFFile.ImageGetHeight()}\n" +
                      $"Format: {Enum.GetName(VTFFile.ImageGetFormat())}\n";

            if (VTFFile.ImageGetHasThumbnail())
            {
                uint w = VTFFile.ImageGetThumbnailWidth();
                uint h = VTFFile.ImageGetThumbnailHeight();
                var f = VTFFile.ImageGetThumbnailFormat();
                var ucharPtr = VTFFile.ImageGetThumbnailData();
                var img = GetImage(ucharPtr, w, h, f);
                img.SaveAsBmp("thumbnail.bmp");
            }
            uint hasImage = VTFFile.ImageGetHasImage();
            if (hasImage != 0)
            {
                uint w = VTFFile.ImageGetWidth();
                uint h = VTFFile.ImageGetHeight();
                var f = VTFFile.ImageGetFormat();
                var ucharPtr = VTFFile.ImageGetData(0, 0, 0, 0);
                var img = GetImage(ucharPtr, w, h, f);
                img.SaveAsBmp("tmp.bmp");
                Image = img;
            }
        }
        else
        {
            var sr = new StreamReader(Stream);
            //todo async
            Content = sr.ReadToEnd();
        }
    }

    private Image GetImage(IntPtr ptr, uint width, uint height, VTFImageFormat format)
    {
        int size = (int)width * (int)height * sizeof(byte) * 4;
        var data = new byte[size];

        GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
        IntPtr pointer = pinnedArray.AddrOfPinnedObject();

        VTFFile.ImageConvertToRGBA8888(ptr, pointer, width, height, format);
        Marshal.Copy(pointer, data, 0, size);

        var img = GetImageFromRgba8888(data, (int)width, (int)height);

        pinnedArray.Free();
        return img;
    }
    private Image GetImageFromRgba8888(byte[] img, int width, int height)
    {
        var asdf = new Rgba32[width * height];
        int j = 0;
        for (int i = 0; i < img.Length; i += 4)
        {
            asdf[j++] = new Rgba32(
                img[i],
                img[i + 1],
                img[i + 2],
                img[i + 3]);
        }

        return Image.LoadPixelData<Rgba32>(asdf.AsSpan(), width, height);
    }

    public void Close()
    {
        if (Stream is not null)
        {
            Stream.Close();
            Stream.Dispose();
            Stream = null;
        }
        Content = "";
    }

    public void Save()
    {
        throw new NotImplementedException("todo save");
        Close();
    }

}
