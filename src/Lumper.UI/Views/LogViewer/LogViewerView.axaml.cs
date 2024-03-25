namespace Lumper.UI.Views.LogViewer;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using NLog;
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

    private void AddLogMessage(LogMessage message)
    {
        if (Logs.Inlines is null)
            return;

        // Trim off top half of the logs once we reach a maximum. From some quick testing,
        // having 500 lines slows down noticeably, and trimming itself is very performant.
        if (_messageCounter >= MaxMessages)
        {
            Logs.Inlines.RemoveRange(0, (Logs.Inlines.Count - 1) / 2);
            _messageCounter /= 2;
        }
        else
        {
            _messageCounter++;
        }

        if (Logs.Inlines.Count > 0)
            Logs.Inlines.Add(new LineBreak());

        Logs.Inlines.Add(new Run(message.Level.ToString().PadRight(8))
        {
            Foreground = LogLevelColors[message.Level]
        });
        Logs.Inlines.Add(new Run(OriginIgnoreRegex().Replace(message.Origin, "").PadRight(48))
        {
            FontStyle = FontStyle.Italic, FontWeight = FontWeight.Medium
        });
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
