﻿using System;
using System.Collections.ObjectModel;
using Lumper.Lib.Tasks;

namespace Lumper.UI.ViewModels.Tasks;

/// <summary>
///     ViewModel for ChangeTextrue Task
/// </summary>
public partial class ChangeTextureTaskViewModel : TaskViewModel
{
    public ChangeTextureTaskViewModel(ChangeTextureTask task)
        : base(task)
    {
        try
        {
            foreach (var item in task.Replace)
                ReplaceItems.Add(new ChangeTextureReplaceItem(this, task, item));
            foreach (var item in task.ReplaceRegex)
                RegexItems.Add(new ChangeTextureRegexItem(this, task, item));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public ObservableCollection<ChangeTextureReplaceItem> ReplaceItems { get; private set; } = new();
    public ObservableCollection<ChangeTextureRegexItem> RegexItems { get; private set; } = new();

    public void AddReplaceItem()
    {
        if (Task is ChangeTextureTask c)
        {
            var replaceItem = new ChangeTextureReplaceItem(this, c);
            if (replaceItem.Add())
                ReplaceItems.Add(replaceItem);
        }
    }

    public void AddRegexItem()
    {
        if (Task is ChangeTextureTask c)
        {
            var regexItem = new ChangeTextureRegexItem(this, c);
            if (regexItem.Add())
                RegexItems.Add(regexItem);
        }
    }

    public void RemoveItem(object o)
    {
        if (o is ChangeTextureRegexItem regexItem)
            RegexItems.Remove(regexItem);
        else if (o is ChangeTextureReplaceItem replaceItem)
            ReplaceItems.Remove(replaceItem);
    }
}
