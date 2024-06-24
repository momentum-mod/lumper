namespace Lumper.UI.Views.LogViewer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
            ViewModel?.Messages
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(AddLogMessage)
                .DisposeWith(disposables));
    }


    private const int MaxMessages = 500;
    private int _messageCounter;
    private LogMessage? _lastMessage;
    private bool _isBatching;

    private void AddLogMessage(LogMessage logMessage)
    {
        if (logMessage.Level == LogLevel.Debug && !(ShowDebug.IsChecked ?? false))
            return;

        try
        {
            if (Logs.Inlines is null)
                return;

            if (_lastMessage == logMessage)
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
                    Logs.Inlines.Add(new Run(" (2)") {
                        FontStyle = FontStyle.Italic,
                        FontWeight = FontWeight.Medium
                    });
                    _isBatching = true;
                }

                return;
            }

            _lastMessage = logMessage;
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

            Logs.Inlines.Add(new Run(logMessage.Level.ToString().PadRight(8)) {
                Foreground = LogLevelColors[logMessage.Level]
            });

            var origin = logMessage.Origin;
            // Don't split off front stuff if not a lumper thing (e.g. a RunExternalToolTask)
            origin = origin.StartsWith("Lumper") ? origin.Split('.')[^1] : origin;
            Logs.Inlines.Add(new Run(origin.PadRight(26)) {
                FontStyle = FontStyle.Italic, FontWeight = FontWeight.Medium, Foreground = Brushes.Gray
            });

            Logs.Inlines.Add(new Run(logMessage.Message));

            if (logMessage.Exception is { } ex)
            {
                Logs.Inlines.Add(new LineBreak());
                Logs.Inlines.Add(new Run($"{new string(' ', 8 + 26)}{ex.GetType().Name}: ") {
                    Foreground = Brushes.Crimson,
                    FontStyle = FontStyle.Italic
                });

                Logs.Inlines.Add(new Run(ex.Message) { Foreground = Brushes.IndianRed });
            }

            if (AutoScroll.IsChecked!.Value)
                ScrollToBottom();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"The logger UI threw an error! Jeepers!! Not throwing it so we don't DIE {e}");
        }
    }

    private static readonly Dictionary<LogLevel, IBrush> LogLevelColors = new() {
        { LogLevel.Info, Brushes.LightBlue },
        { LogLevel.Warn, Brushes.Orange },
        { LogLevel.Error, Brushes.Red },
        { LogLevel.Fatal, Brushes.Fuchsia },
        { LogLevel.Debug, Brushes.Aqua },
        { LogLevel.Trace, Brushes.Chartreuse },
        { LogLevel.Off, Brushes.Gray }
    };

    private void ScrollToBottom(object? _, RoutedEventArgs __) => ScrollToBottom();
    private void ScrollToBottom() => ScrollViewer.ScrollToEnd();
}
