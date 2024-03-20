namespace Lumper.UI.ViewModels.Bsp.Lumps.Entity;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DynamicData;
using Lumper.Lib.BSP.Lumps.BspLumps;
using ReactiveUI;

/// <summary>
///     ViewModel for <see cref="EntityLump" />.
/// </summary>
public class EntityLumpViewModel : LumpBase, System.IDisposable
{
    private readonly SourceList<EntityViewModel> _entities = new();
    private readonly EntityLump _entityLump;

    public EntityLumpViewModel(BspViewModel parent, EntityLump entityLump)
        : base(parent)
    {
        _entityLump = entityLump;
        Init();
        Open();

        InitializeNodeChildrenObserver(_entities);
    }

    public override string NodeName => "Entity Group";

    public override BspNodeBase? ViewNode => this;


    private bool contentChanged;
    private string _content = "";
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                contentChanged = true;
                this.RaisePropertyChanged();
            }
        }
    }

    private void Init()
    {
        _entities.Clear();
        foreach (Lib.BSP.Struct.Entity entity in _entityLump.Data)
            _entities.Add(new EntityViewModel(this, entity));
    }


    public override void Open()
    {
        using var mem = new MemoryStream();
        _entityLump.Write(mem);
        mem.Seek(0, SeekOrigin.Begin);
        _content = Encoding.ASCII.GetString(mem.ToArray());
    }

    public void Save()
    {
        if (contentChanged)
        {
            using var mem = new MemoryStream(Encoding.ASCII.GetBytes(_content));
            mem.Seek(0, SeekOrigin.Begin);
            using var reader = new BinaryReader(mem);
            _entityLump.Data.Clear();
            _entityLump.Read(reader, mem.Length);
            Init();
            contentChanged = false;
        }
    }

    public void AddEntity()
    {
        var properties = new List<KeyValuePair<string, string>>();
        var entity = new Lib.BSP.Struct.Entity(properties);
        _entityLump.Data.Add(entity);
        _entities.Add(new EntityViewModel(this, entity));
    }

    public void Dispose() => throw new System.NotImplementedException();
}
