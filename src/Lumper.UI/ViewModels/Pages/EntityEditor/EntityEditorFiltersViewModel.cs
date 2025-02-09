namespace Lumper.UI.ViewModels.Pages.EntityEditor;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Lib.Bsp.Struct;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public class EntityEditorFilters : ReactiveObject
{
    [Reactive]
    public string Classname { get; set; } = "";

    [Reactive]
    public string Key { get; set; } = "";

    [Reactive]
    public string Value { get; set; } = "";

    [Reactive]
    public bool WildcardWrapping { get; set; } = true;

    [Reactive]
    public bool ShowBrushEntities { get; set; } = true;

    [Reactive]
    public bool ShowPointEntities { get; set; } = true;

    [Reactive]
    public string SpherePosition { get; set; } = "";

    [Reactive]
    public string SphereRadius { get; set; } = "";

    public bool TryParseSphere([NotNullWhen(true)] out (Vector3 pos, int radius)? location)
    {
        location = null;
        if (string.IsNullOrWhiteSpace(SpherePosition) || string.IsNullOrWhiteSpace(SphereRadius))
            return false;

        if (!Entity.TryParsePosition(SpherePosition, out Vector3? vec))
            return false;

        if (!int.TryParse(SphereRadius, out int radius))
            return false;

        location = (vec.Value, radius);
        return true;
    }
}
