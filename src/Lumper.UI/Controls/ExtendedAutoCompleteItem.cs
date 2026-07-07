namespace Lumper.UI.Controls;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ExtendedAutoCompleteItem : INotifyPropertyChanged
{
    private bool _isChecked;
    private object? _value;
    private string? _display;

    public object? Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string? Display
    {
        get => _display;
        set
        {
            if (_display != value)
            {
                _display = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string DisplayText => string.IsNullOrEmpty(Display) ? Value?.ToString() ?? string.Empty : Display;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
