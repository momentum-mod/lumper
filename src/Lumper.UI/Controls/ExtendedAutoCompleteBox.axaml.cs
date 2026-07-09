namespace Lumper.UI.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

public partial class ExtendedAutoCompleteBox : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<
        ExtendedAutoCompleteBox,
        string
    >(nameof(Text));

    public static readonly StyledProperty<IReadOnlyCollection<ExtendedAutoCompleteItem>> SuggestionsProperty =
        AvaloniaProperty.Register<ExtendedAutoCompleteBox, IReadOnlyCollection<ExtendedAutoCompleteItem>>(
            nameof(Suggestions)
        );

    public static readonly StyledProperty<bool> IsBitfieldModeProperty = AvaloniaProperty.Register<
        ExtendedAutoCompleteBox,
        bool
    >(nameof(IsBitfieldMode));

    public static readonly StyledProperty<bool> IsDropdownOpenProperty = AvaloniaProperty.Register<
        ExtendedAutoCompleteBox,
        bool
    >(nameof(IsDropdownOpen));

    private bool _isUpdatingText;
    private bool _isNavigating;
    private bool _isInitializing;
    private long _preservedBits;
    private INotifyCollectionChanged? _subscribedCollection;
    private readonly HashSet<ExtendedAutoCompleteItem> _subscribedItems = [];

    public ObservableCollection<ExtendedAutoCompleteItem> FilteredSuggestions { get; } = [];

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IReadOnlyCollection<ExtendedAutoCompleteItem> Suggestions
    {
        get => GetValue(SuggestionsProperty);
        set => SetValue(SuggestionsProperty, value);
    }

    public bool IsBitfieldMode
    {
        get => GetValue(IsBitfieldModeProperty);
        set => SetValue(IsBitfieldModeProperty, value);
    }

    public bool IsDropdownOpen
    {
        get => GetValue(IsDropdownOpenProperty);
        set => SetValue(IsDropdownOpenProperty, value);
    }

    static ExtendedAutoCompleteBox()
    {
        TextProperty.Changed.AddClassHandler<ExtendedAutoCompleteBox>((x, e) => x.OnTextChanged());
        SuggestionsProperty.Changed.AddClassHandler<ExtendedAutoCompleteBox>((x, e) => x.OnSuggestionsChanged(e));
        IsBitfieldModeProperty.Changed.AddClassHandler<ExtendedAutoCompleteBox>((x, e) => x.OnIsBitfieldModeChanged());
    }

    public ExtendedAutoCompleteBox()
    {
        InitializeComponent();

        // The visual tree is fixed for the lifetime of this control (unlike a
        // TemplatedControl, there's no re-templating to guard against), so these
        // elements are wired once here instead of in OnApplyTemplate, and referenced
        // directly rather than through nullable backing fields.
        DropdownButton.Click += (_, _) => ToggleDropdown();

        InputTextBox.KeyDown += OnTextBoxKeyDown;
        InputTextBox.GotFocus += (_, _) =>
        {
            if (!IsBitfieldMode)
                UpdateFilteredSuggestions();
        };

        StandardList.SelectionChanged += OnStandardListSelectionChanged;
        StandardList.ItemsSource = FilteredSuggestions;
        StandardList.PointerWheelChanged += OnListBoxPointerWheelChanged;

        BitfieldList.ItemsSource = FilteredSuggestions;
        BitfieldList.PointerWheelChanged += OnListBoxPointerWheelChanged;

        SetDropdownButtonState();
        UpdateFilteredSuggestions();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeCollection(Suggestions);
        SubscribeAllItems(Suggestions);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeCollection();
        UnsubscribeAllItems();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnListBoxPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void SetDropdownButtonState()
    {
        DropdownButton.IsVisible = Suggestions is { Count: > 0 };
    }

    private void ToggleDropdown()
    {
        IsDropdownOpen = !IsDropdownOpen;
        if (IsDropdownOpen)
        {
            UpdateFilteredSuggestions(showAll: true);
            InputTextBox.Focus();
        }
    }

    private void OnIsBitfieldModeChanged()
    {
        UpdateFilteredSuggestions(showAll: IsBitfieldMode);

        if (IsBitfieldMode)
        {
            InitializeBitfieldCheckboxes();
            CalculateBitfieldSum();
        }
        else
        {
            IsDropdownOpen = false;
        }
    }

    private void SubscribeCollection(IReadOnlyCollection<ExtendedAutoCompleteItem>? suggestions)
    {
        if (suggestions is INotifyCollectionChanged incc)
        {
            if (ReferenceEquals(incc, _subscribedCollection))
                return;

            UnsubscribeCollection();
            incc.CollectionChanged += OnSuggestionsCollectionChanged;
            _subscribedCollection = incc;
        }
        else
        {
            UnsubscribeCollection();
        }
    }

    private void UnsubscribeCollection()
    {
        if (_subscribedCollection != null)
        {
            _subscribedCollection.CollectionChanged -= OnSuggestionsCollectionChanged;
            _subscribedCollection = null;
        }
    }

    private void SubscribeItem(ExtendedAutoCompleteItem item)
    {
        if (_subscribedItems.Add(item))
            item.PropertyChanged += OnItemPropertyChanged;
    }

    private void UnsubscribeItem(ExtendedAutoCompleteItem item)
    {
        if (_subscribedItems.Remove(item))
            item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void SubscribeAllItems(IEnumerable<ExtendedAutoCompleteItem>? items)
    {
        if (items == null)
            return;

        foreach (ExtendedAutoCompleteItem item in items)
            SubscribeItem(item);
    }

    private void UnsubscribeAllItems()
    {
        foreach (ExtendedAutoCompleteItem item in _subscribedItems)
            item.PropertyChanged -= OnItemPropertyChanged;
        _subscribedItems.Clear();
    }

    private void OnSuggestionsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        SubscribeCollection(e.NewValue as IReadOnlyCollection<ExtendedAutoCompleteItem>);

        UnsubscribeAllItems();
        SubscribeAllItems(e.NewValue as IEnumerable<ExtendedAutoCompleteItem>);

        UpdateFilteredSuggestions();
        SetDropdownButtonState();

        if (IsBitfieldMode)
            InitializeBitfieldCheckboxes();
    }

    private void OnSuggestionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset || (e.OldItems == null && e.NewItems == null))
        {
            UnsubscribeAllItems();
            SubscribeAllItems(Suggestions);
        }
        else
        {
            if (e.OldItems != null)
            {
                foreach (ExtendedAutoCompleteItem item in e.OldItems)
                    UnsubscribeItem(item);
            }
            if (e.NewItems != null)
            {
                foreach (ExtendedAutoCompleteItem item in e.NewItems)
                    SubscribeItem(item);
            }
        }

        UpdateFilteredSuggestions();
        SetDropdownButtonState();

        if (IsBitfieldMode)
            InitializeBitfieldCheckboxes();
    }

    private void OnTextChanged()
    {
        if (_isUpdatingText)
            return;

        if (IsBitfieldMode)
        {
            InitializeBitfieldCheckboxes();
        }
        else
        {
            UpdateFilteredSuggestions();
            if (!string.IsNullOrEmpty(Text) && InputTextBox.IsFocused)
            {
                IsDropdownOpen = FilteredSuggestions.Any();
            }
        }
    }

    private void UpdateFilteredSuggestions(bool showAll = false)
    {
        if (Suggestions == null)
        {
            FilteredSuggestions.Clear();
            return;
        }

        FilteredSuggestions.Clear();
        string query = Text ?? string.Empty;

        IEnumerable<ExtendedAutoCompleteItem> toAdd =
            (showAll || IsBitfieldMode || string.IsNullOrEmpty(query))
                ? Suggestions
                : Suggestions.Where(x => x.DisplayText.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (ExtendedAutoCompleteItem item in toAdd)
        {
            FilteredSuggestions.Add(item);
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsBitfieldMode && e.PropertyName == nameof(ExtendedAutoCompleteItem.IsChecked))
        {
            CalculateBitfieldSum();
        }
        else if (
            e.PropertyName is (nameof(ExtendedAutoCompleteItem.Display)) or (nameof(ExtendedAutoCompleteItem.Value))
        )
        {
            UpdateFilteredSuggestions(showAll: IsBitfieldMode || IsDropdownOpen);
        }
    }

    private void InitializeBitfieldCheckboxes()
    {
        _isInitializing = true;
        try
        {
            _preservedBits = 0;

            if (Suggestions == null || string.IsNullOrEmpty(Text))
                return;

            if (!long.TryParse(Text, out long currentBitfieldValue))
                return;

            long knownMask = 0;
            foreach (ExtendedAutoCompleteItem item in Suggestions)
            {
                if (item.Value != null && long.TryParse(item.Value.ToString(), out long flagValue) && flagValue != 0)
                {
                    knownMask |= flagValue;
                }
            }

            _preservedBits = currentBitfieldValue & ~knownMask;

            foreach (ExtendedAutoCompleteItem item in Suggestions)
            {
                if (item.Value != null && long.TryParse(item.Value.ToString(), out long flagValue))
                {
                    item.IsChecked =
                        flagValue != 0 ? (currentBitfieldValue & flagValue) == flagValue : currentBitfieldValue == 0;
                }
            }
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void CalculateBitfieldSum()
    {
        if (_isInitializing)
            return;

        long sum = _preservedBits;
        if (Suggestions != null)
        {
            foreach (ExtendedAutoCompleteItem item in Suggestions)
            {
                if (item.IsChecked && item.Value != null)
                {
                    if (long.TryParse(item.Value.ToString(), out long val))
                    {
                        sum |= val;
                    }
                }
            }
        }

        if (long.TryParse(Text, out long currentValue) && currentValue == sum)
            return;

        _isUpdatingText = true;
        Text = sum.ToString(CultureInfo.InvariantCulture);
        _isUpdatingText = false;
    }

    private void OnStandardListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (StandardList.SelectedItem is ExtendedAutoCompleteItem item)
        {
            _isUpdatingText = true;
            Text = item.Value?.ToString() ?? item.DisplayText;
            _isUpdatingText = false;

            bool shouldCloseDropdown = !_isNavigating;

            Dispatcher.UIThread.Post(() =>
            {
                InputTextBox.CaretIndex = Text?.Length ?? 0;
                if (shouldCloseDropdown)
                {
                    IsDropdownOpen = false;
                    StandardList.SelectedItem = null;
                    InputTextBox.Focus();
                }
            });
        }
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        ListBox activeList = IsBitfieldMode ? BitfieldList : StandardList;
        if (!FilteredSuggestions.Any())
            return;

        if (e.Key == Key.Down)
        {
            _isNavigating = true;
            if (!IsDropdownOpen)
                IsDropdownOpen = true;
            activeList.SelectedIndex = Math.Min(activeList.SelectedIndex + 1, FilteredSuggestions.Count - 1);
            e.Handled = true;
            _isNavigating = false;
        }
        else if (e.Key == Key.Up)
        {
            _isNavigating = true;
            activeList.SelectedIndex = Math.Max(activeList.SelectedIndex - 1, 0);
            e.Handled = true;
            _isNavigating = false;
        }
        else if (e.Key == Key.Enter)
        {
            if (IsBitfieldMode && activeList.SelectedItem is ExtendedAutoCompleteItem bitItem)
            {
                bitItem.IsChecked = !bitItem.IsChecked;
                e.Handled = true;
            }
            else if (!IsBitfieldMode && activeList.SelectedItem is ExtendedAutoCompleteItem stdItem)
            {
                _isUpdatingText = true;
                Text = stdItem.Value?.ToString() ?? stdItem.DisplayText;
                _isUpdatingText = false;

                IsDropdownOpen = false;
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            IsDropdownOpen = false;
            e.Handled = true;
        }
    }
}
