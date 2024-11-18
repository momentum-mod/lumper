namespace Lumper.UI.ViewModels.LogViewer;

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using ViewModels;
using Views.LogViewer;

public sealed class LogViewerViewModel : ViewModelWithView<LogViewerViewModel, LogViewerView>, IDisposable
{
    // Subject is an easy way to get reactive NLog output. Any thread can call AddLog, but the view listens to the
    // subject on the main thread. ReplaySubject is used because if someone loads a map with a launch arg, the loading
    // is allowed to get started in its own thread before the view is fully initialized. ReplaySubject will buffer
    // and replay allow messages recorded before the view is ready.
    private readonly ReplaySubject<LogMessage> _messageSubject = new();
    public IObservable<LogMessage> Messages => _messageSubject.AsObservable();

    public LogViewerViewModel() =>
        LogManager
            .Setup()
            .LoadConfiguration(builder => builder.ForLogger().WriteToMethodCall((logEvent, _) => AddLog(logEvent)));

    private void AddLog(LogEventInfo e) =>
        _messageSubject.OnNext(
            new LogMessage
            {
                Level = e.Level,
                Message = e.Message,
                Origin = e.LoggerName ?? "Unknown Origin",
                Exception = e.Exception,
            }
        );

    public void Dispose() => _messageSubject.Dispose();
}

public record LogMessage
{
    public required string Message { get; init; }
    public required LogLevel Level { get; init; }
    public required string Origin { get; init; }
    public required Exception? Exception { get; init; }
}
