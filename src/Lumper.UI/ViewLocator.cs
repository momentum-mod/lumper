namespace Lumper.UI;

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ViewModels;

/// <summary>
/// Insane but simple ViewLocator implementation, derived from here:
/// https://docs.avaloniaui.net/docs/tutorials/todo-list-app/locating-views
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control Build(object? param)
    {
        if (param is null)
            return new TextBlock { Text = "Null referenced object" };

        var name = param.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
            return (Control)Activator.CreateInstance(type)!;

        return new TextBlock { Text = $"View not found: {name}" };
    }

    public bool Match(object? data) => data is ViewModel;
}
