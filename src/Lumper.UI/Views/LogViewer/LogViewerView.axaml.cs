namespace Lumper.UI.Views.LogViewer;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using DynamicData.Binding;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.LogViewer;
using NLog;
using ReactiveUI;

public partial class LogViewerView : ReactiveUserControl<LogViewerViewModel>
{
    private readonly ObservableCollectionExtended<SelectableTextBlock> _lines = [];
    private const int MaxMessages = 500;
    private int _messageCounter;
    private LogMessage? _lastMessage;
    private bool _isBatching;

    public LogViewerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            LogLines.ItemsSource = _lines;
            ViewModel?.Messages.ObserveOn(RxApp.MainThreadScheduler).Subscribe(AddLogMessage).DisposeWith(disposables);
        });
    }

    private void AddLogMessage(LogMessage logMessage)
    {
        if (logMessage.Level == LogLevel.Debug && !StateService.Instance.LogShowDebug)
            return;

        try
        {
            SelectableTextBlock newLine = new();
            if (LogLines is null || newLine.Inlines is null)
                return;

            if (_lastMessage == logMessage)
            {
                if (_isBatching)
                {
                    SelectableTextBlock counter = _lines[^1];
                    if (counter.Text is null)
                        return;
                    counter.Text = $" ({int.Parse(counter.Text[2..^1], CultureInfo.InvariantCulture) + 1})";
                }
                else
                {
                    newLine.Inlines!.Add(
                        new Run(" (2)") { FontStyle = FontStyle.Italic, FontWeight = FontWeight.Medium }
                    );
                    _isBatching = true;
                }

                return;
            }

            _lastMessage = logMessage;
            _isBatching = false;

            // Trim off top half of the logs once we reach a maximum. From some quick testing,
            // having 500 lines slows down noticeably, and trimming itself is very performant.
            if (_messageCounter >= MaxMessages)
            {
                using IDisposable _ = _lines.SuspendNotifications();
                _lines.RemoveRange(0, newLine.Inlines.Count / 2);
                _messageCounter /= 2;
            }
            else
            {
                _messageCounter++;
            }

            newLine.Inlines.Add(
                new Run(logMessage.Level.ToString().PadRight(8)) { Foreground = LogLevelColors[logMessage.Level] }
            );

            string origin = logMessage.Origin;
            // Don't split off front stuff if not a lumper thing (e.g. a RunExternalToolTask)
            origin = origin.StartsWith("Lumper", StringComparison.Ordinal) ? origin.Split('.')[^1] : origin;
            newLine.Inlines.Add(
                new Run(origin.PadRight(26))
                {
                    FontStyle = FontStyle.Italic,
                    FontWeight = FontWeight.Medium,
                    Foreground = Brushes.LightGray,
                }
            );

            newLine.Inlines.Add(
                new Run(logMessage.Message)
                {
                    Foreground = logMessage.Exception is null ? Brushes.White : Brushes.LightPink,
                }
            );

            if (logMessage.Exception is { } ex)
            {
                newLine.Inlines.Add(new LineBreak());
                newLine.Inlines.Add(
                    new Run($"{new string(' ', 8 + 26)}{ex.GetType().Name}: ")
                    {
                        Foreground = Brushes.Crimson,
                        FontStyle = FontStyle.Italic,
                    }
                );

                newLine.Inlines.Add(new Run(ex.Message) { Foreground = Brushes.IndianRed });
                if (ex.StackTrace is not null)
                {
                    newLine.Inlines.Add(new LineBreak());
                    string indent = new(' ', 8 + 26);
                    newLine.Inlines.Add(
                        new Run(indent + ex.StackTrace.Replace("\n", "\n" + indent)) { Foreground = Brushes.Tomato }
                    );
                }
            }

            _lines.Add(newLine);

            if (StateService.Instance.LogAutoScroll)
                ScrollToBottom();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"The logger UI threw an error! Jeepers!! Not throwing it so we don't DIE {e}");
        }
    }

    private static readonly Dictionary<LogLevel, IBrush> LogLevelColors = new()
    {
        { LogLevel.Info, Brushes.LightBlue },
        { LogLevel.Warn, Brushes.Orange },
        { LogLevel.Error, Brushes.Red },
        { LogLevel.Fatal, Brushes.Fuchsia },
        { LogLevel.Debug, Brushes.Aqua },
        { LogLevel.Trace, Brushes.Chartreuse },
        { LogLevel.Off, Brushes.Gray },
    };

    private void ScrollToBottom(object? _, RoutedEventArgs __)
    {
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        ScrollViewer.ScrollToEnd();
    }
}
