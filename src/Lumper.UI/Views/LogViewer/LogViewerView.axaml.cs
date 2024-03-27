namespace Lumper.UI.Views.LogViewer;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using NLog;
using NLog.Fluent;
using ReactiveUI;
using ViewModels.LogViewer;

public partial class LogViewerView : ReactiveUserControl<LogViewerViewModel>
{
    public LogViewerView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
            ViewModel?.Messages.Subscribe(AddLogMessage).DisposeWith(disposables));
    }

    private const int MaxMessages = 500;
    private int _messageCounter;
    private LogMessage? _lastMessage;
    private bool _isBatching;

    // Note that errors here are not logged, they'll just cause the application to crash.
    // So no danger of infinite loops :)
    private void AddLogMessage(LogMessage message)
    {
        if (Logs.Inlines is null)
            return;

        if (_lastMessage == message)
        {
            if (_isBatching)
            {
                var counter = Logs.Inlines.Last() as Run;
                if (counter?.Text is null)
                    return;
                counter.Text = $" ({int.Parse(counter.Text[2..^1]) + 1})";
            }
            else
            {
                Logs.Inlines.Add(new Run(" (2)") { FontStyle = FontStyle.Italic, FontWeight = FontWeight.Medium });
                _isBatching = true;
            }

            return;
        }

        _lastMessage = message;
        _isBatching = false;

        // Trim off top half of the logs once we reach a maximum. From some quick testing,
        // having 500 lines slows down noticeably, and trimming itself is very performant.
        // Inline count might be odd, so half a line gets trimmed, not a big deal if that happens.
        if (_messageCounter >= MaxMessages)
        {
            Logs.Inlines.RemoveRange(0, Logs.Inlines.Count / 2);
            _messageCounter /= 2;
        }
        else
        {
            _messageCounter++;
        }

        if (Logs.Inlines.Count > 0)
            Logs.Inlines.Add(new LineBreak());

        Logs.Inlines.Add(new Run(message.Level.ToString().PadRight(8)) { Foreground = LogLevelColors[message.Level] });
        Logs.Inlines.Add(new Run(OriginIgnoreRegex().Replace(message.Origin, "").PadRight(48)) { FontStyle = FontStyle.Italic, FontWeight = FontWeight.Medium });
        Logs.Inlines.Add(new Run(message.Message));

        if (AutoScroll.IsChecked!.Value)
            ScrollToBottom();
    }

    private static readonly Dictionary<LogLevel, IBrush> LogLevelColors = new()
    {
        { LogLevel.Info,  Brushes.LightBlue },
        { LogLevel.Warn,  Brushes.Orange },
        { LogLevel.Error, Brushes.Red },
        { LogLevel.Fatal, Brushes.Fuchsia },
        { LogLevel.Debug, Brushes.Aqua },
        { LogLevel.Trace, Brushes.Chartreuse },
        { LogLevel.Off,   Brushes.Gray }
    };

    private void ScrollToBottom(object? _, RoutedEventArgs __) => ScrollToBottom();
    private void ScrollToBottom() => ScrollViewer.ScrollToEnd();

    [GeneratedRegex("^Lumper\\.")]
    private static partial Regex OriginIgnoreRegex();
}
