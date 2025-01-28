namespace Lumper.UI.ViewModels.Pages.PakfileExplorer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Lumper.UI.ViewModels.Shared.Pakfile;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Node = PakfileTreeNodeViewModel;
using PathList = System.Collections.Generic.List<string>;

public class PakfileTreeViewModel
{
    public Node Root { get; } = new(null, true) { Name = "__ROOT__" };

    public PakfileTreeViewModel(SourceCache<PakfileEntryViewModel, string> source) =>
        source
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(changes =>
            {
                foreach (Change<PakfileEntryViewModel, string> change in changes)
                {
                    switch (change.Reason)
                    {
                        case ChangeReason.Add:
                            Root.Add(change.Current, [.. change.Key.Split('/')]);
                            break;
                        case ChangeReason.Remove:
                            Root.RemoveRecursive([.. change.Key.Split('/')]);
                            break;
                    }
                }
            });

    public Node? Find(PathList path)
    {
        Node? node = Root;
        foreach (string p in path)
        {
            node = node.Children?.FirstOrDefault(x => x.Name == p);

            if (node is null)
                return null;
        }

        return node;
    }

    public static void FixParentsRecursive(Node node)
    {
        foreach (Node n in node.Children ?? [])
        {
            n.Parent = node;
            if (n.Children is not null)
                FixParentsRecursive(n);
        }
    }

    public IEnumerable<Node> EnumerateLeaves() => Root.EnumerateLeaves();
}

public class PakfileTreeNodeViewModel : ViewModel
{
    public PakfileEntryViewModel? Leaf { get; init; }

    public Node? Parent { get; set; }

    public ObservableCollectionExtended<Node>? Children { get; private set; }

    [Reactive]
    public required string Name { get; set; }

    [Reactive]
    public long? Size { get; set; }

    [Reactive]
    public bool IsExpanded { get; set; }

    public bool IsDirectory => Children is not null;

    public string? Extension => Leaf?.Extension;

    public PakfileTreeNodeViewModel(Node? parent, bool root)
    {
        IsExpanded = root;
        Size = Leaf?.CompressedSize ?? 0;

        if (root)
            Children = [];
        else
            Parent = parent;
    }

    // Note that this is the entire path, INCLUDING name.extension
    public PathList Path
    {
        get
        {
            Node node = this;
            PathList path = [];
            while (node.Parent is not null)
            {
                path.Add(node.Name);
                node = node.Parent;
            }

            path.Reverse();
            return path;
        }
    }

    public string PathString => string.Join("/", Path);

    public void Add(PakfileEntryViewModel value, PathList path) => AddInternal(value, path);

    public void AddDirectory(PathList path) => AddInternal(null, path);

    private void AddInternal(PakfileEntryViewModel? value, PathList path)
    {
        long size = value?.CompressedSize ?? 0;

        // Processing directory paths of the path, recursing down the tree and creating new nodes where needed
        if (path.Count > 1)
        {
            Node? existing = Children?.ToList().Find(x => x.Name == path[0]);
            if (existing is not null)
            {
                existing.AddInternal(value, path[1..]);
                existing.Size += size;
                return;
            }

            var newChild = new Node(this, root: false) { Name = path[0], Size = size };
            newChild.AddInternal(value, path[1..]);
            (Children ??= []).Add(newChild);
            return;
        }

        // Okay, actually the filename, create the node
        // `value` being null here is fine, just means we're creating a directory. Those don't actually get saved out
        // (zips can't have empty directories), but UI uses them.
        var newNode = new Node(this, root: false)
        {
            Name = path[0],
            Leaf = value,
            Size = size,
        };
        if (value is null)
            newNode.Children = [];
        (Children ??= []).Add(newNode);
    }

    public void RemoveSelf() => Parent?.Children?.Remove(this);

    public void RemoveRecursive(PathList path)
    {
        Node? node = Children?.FirstOrDefault(child => child.Name == path[0]);

        if (node is null)
            return;

        if (path.Count > 1)
        {
            node.RemoveRecursive(path[1..]);

            node.RecalculateSize();
            // Delete directory is no children is empty.
            // Even though zips don't have actual directories we *do* let you add
            // empty directories (so you can add stuff to them), so maybe this isn't quite the
            // behaviour we want. However without this, clearing a pakfile (like when replacing contents)
            // won't delete the directory nodes...
            if (node.Children is { Count: 0 })
                Children!.Remove(node);

            return;
        }

        Children!.Remove(node);
    }

    public IEnumerable<Node> EnumerateLeaves()
    {
        Stack<Node> stack = new([this]);
        while (stack.Count > 0)
        {
            Node node = stack.Pop();
            foreach (Node child in node.Children ?? [])
                stack.Push(child);

            if (node.Leaf is not null)
                yield return node;
        }
    }

    public List<Node> GetDescendants()
    {
        Node? parent = Parent;
        List<Node> arr = [];
        while (parent is not null)
        {
            arr.Add(parent);
            parent = parent.Parent;
        }

        return arr;
    }

    public static Node FindCommonAncestor(IEnumerable<Node> nodes)
    {
        // Could definitely be faster
        var descendants = nodes
            .Select(node => node.GetDescendants())
            .OrderBy(x => x.Count) // Shortest first
            .ToList();

        Node? previous = null;
        for (int i = 1; i <= descendants[0].Count; i++)
        {
            Node curr = descendants[0][^i];
            if (descendants[1..].Any(list => list[^i] != curr))
                return previous ?? curr;
            previous = curr;
        }

        return previous!;
    }

    public void RecalculateSize() => Size = Children?.Sum(child => child.Size) ?? 0;

    public static Comparison<Node?> SortAscending<T>(Func<Node, T> selector) =>
        (x, y) =>
        {
            if (x is null && y is null)
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;
            return Comparer<T>.Default.Compare(selector(x), selector(y));
        };

    public static Comparison<Node?> SortDescending<T>(Func<Node, T> selector) =>
        (x, y) =>
        {
            if (x is null && y is null)
                return 0;
            if (x is null)
                return 1;
            if (y is null)
                return -1;
            return Comparer<T>.Default.Compare(selector(y), selector(x));
        };
}
