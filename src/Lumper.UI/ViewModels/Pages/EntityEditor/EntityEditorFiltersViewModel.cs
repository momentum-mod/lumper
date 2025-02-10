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

    /// <summary>
    /// Parse a CSV string into filter function that matchs entity value strings against the filter.
    /// Strings prepended with '-' exclude entities matching the string.
    /// </summary>
    public static bool TryParseStringFilters(
        string filter,
        [NotNullWhen(true)] out List<string>? incl,
        [NotNullWhen(true)] out List<string>? excl
    )
    {
        incl = null;
        excl = null;

        if (string.IsNullOrWhiteSpace(filter))
            return false;

        var split = filter.Split(',').Select(s => s.Trim()).ToList();
        if (split.Count == 0)
            return false;

        incl = split.Where(s => !s.StartsWith('-')).ToList();
        excl = split.Where(s => s.StartsWith('-')).Select(s => s[1..]).ToList();

        return true;
    }

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
