using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Lumper.UI.ViewModels.Bsp.Lumps.Entity;

namespace Lumper.UI.Views.Bsp.Lumps.Entity;

public class EntityPropertyTemplateSelector : IDataTemplate
{
    [Content] public Dictionary<EntityPropertyBase, IDataTemplate> Templates { get; } = new();

    public IControl Build(object data)
    {
        return Templates[(EntityPropertyBase)data].Build(data);
    }

    public bool Match(object data)
    {
        return data is EntityPropertyBase;
    }
}