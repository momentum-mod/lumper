namespace Lumper.UI;
using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Lumper.UI.ViewModels;

public class ViewLocator : IDataTemplate
{
    public static IControl Build(object? data)
    {
        if (data is null)
            return new TextBlock { Text = "Null referenced object" };

        var name = data.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
            return (Control)Activator.CreateInstance(type)!;

        return new TextBlock { Text = $"Not Found: {name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
