using System;
using System.IO;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Controls.ApplicationLifetimes;
using Lumper.UI.ViewModels.Bsp.Lumps.PakFile;

namespace Lumper.UI.Views.Bsp.Lumps.PakFile;

public partial class PakFileEntryView : UserControl
{
    public PakFileEntryView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

}
