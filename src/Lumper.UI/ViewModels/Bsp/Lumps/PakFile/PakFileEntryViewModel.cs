using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DynamicData;
using SharpCompress.Archives.Zip;
using ReactiveUI;

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

    public override bool IsModified =>
        Nodes is { Count: > 0 } && Nodes.Any(n => n.IsModified);


    public void Open()
    {
        Stream = _entry.OpenEntryStream();
        var sr = new StreamReader(Stream);
        //todo async
        Content = sr.ReadToEnd();
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
