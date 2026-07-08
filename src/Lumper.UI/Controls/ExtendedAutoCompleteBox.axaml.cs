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
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;

public class ExtendedAutoCompleteBox : TemplatedControl
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

    private TextBox? _textBox;
    private Button? _dropdownButton;
    private ListBox? _standardList;
    private ListBox? _bitfieldList;
    private bool _isUpdatingText;
    private bool _isNavigating;
    private bool _isInitializing;
    private long _preservedBits;

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

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        _dropdownButton = e.NameScope.Find<Button>("PART_DropdownButton");
        _standardList = e.NameScope.Find<ListBox>("PART_StandardList");
        _bitfieldList = e.NameScope.Find<ListBox>("PART_BitfieldList");

        if (_dropdownButton != null)
        {
            _dropdownButton.Click += (s, args) => ToggleDropdown();
        }

        if (_textBox != null)
        {
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.GotFocus += (s, args) =>
            {
                if (!IsBitfieldMode)
                    UpdateFilteredSuggestions();
            };
        }

        if (_standardList != null)
        {
            _standardList.SelectionChanged += OnStandardListSelectionChanged;
            _standardList.ItemsSource = FilteredSuggestions;
            _standardList.PointerWheelChanged += OnListBoxPointerWheelChanged;
        }

        if (_bitfieldList != null)
        {
            _bitfieldList.ItemsSource = FilteredSuggestions;
            _bitfieldList.PointerWheelChanged += OnListBoxPointerWheelChanged;
        }

        SetDropdownButtonState();
        UpdateFilteredSuggestions();
    }

    private void OnListBoxPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void SetDropdownButtonState()
    {
        if (_dropdownButton == null)
            return;
        _dropdownButton.IsVisible = Suggestions is { Count: > 0 };
    }

    private void ToggleDropdown()
    {
        IsDropdownOpen = !IsDropdownOpen;
        if (IsDropdownOpen)
        {
            UpdateFilteredSuggestions(showAll: true);
            _textBox?.Focus();
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

    private void OnSuggestionsChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnSuggestionsCollectionChanged;
        }
        if (e.OldValue is IEnumerable<ExtendedAutoCompleteItem> oldItems)
        {
            foreach (ExtendedAutoCompleteItem item in oldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += OnSuggestionsCollectionChanged;
        }
        if (e.NewValue is IEnumerable<ExtendedAutoCompleteItem> newItems)
        {
            foreach (ExtendedAutoCompleteItem item in newItems)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        UpdateFilteredSuggestions();
        SetDropdownButtonState();

        if (IsBitfieldMode)
            InitializeBitfieldCheckboxes();
    }

    private void OnSuggestionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ExtendedAutoCompleteItem item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
        }
        if (e.NewItems != null)
        {
            foreach (ExtendedAutoCompleteItem item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        UpdateFilteredSuggestions();

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
            if (!string.IsNullOrEmpty(Text) && _textBox != null && _textBox.IsFocused)
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
                if (
                    item.Value != null
                    && long.TryParse(item.Value.ToString(), out long flagValue)
                    && flagValue != 0
                )
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
                        flagValue != 0
                            ? (currentBitfieldValue & flagValue) == flagValue
                            : currentBitfieldValue == 0;
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
        if (_standardList?.SelectedItem is ExtendedAutoCompleteItem item)
        {
            _isUpdatingText = true;
            Text = item.Value?.ToString() ?? item.DisplayText;
            _isUpdatingText = false;

            if (!_isNavigating)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsDropdownOpen = false;
                    _standardList.SelectedItem = null;
                    _textBox?.Focus();
                });
            }
        }
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        ListBox? activeList = IsBitfieldMode ? _bitfieldList : _standardList;
        if (activeList == null || !FilteredSuggestions.Any())
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
