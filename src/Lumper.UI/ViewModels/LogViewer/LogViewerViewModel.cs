namespace Lumper.UI.ViewModels.LogViewer;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lumper.UI.ViewModels;
using NLog;
using ReactiveUI;

public class LogViewerViewModel : ViewModelBase, IDisposable
{
    // AddLog could be called from any thread in the application, so Subject lets
    // us collect up all those calls then observe on the UI thread
    private readonly Subject<LogMessage> _messageSubject = new();
    public IObservable<LogMessage> Messages =>
        _messageSubject
            .ObserveOn(RxApp.MainThreadScheduler)
            .AsObservable();

    public void AddLog(LogEventInfo e) =>
        _messageSubject.OnNext(new LogMessage { Level = e.Level, Message = e.Message, Origin = e.LoggerName ?? "Unknown Origin" });

    public void Dispose() => _messageSubject.Dispose();
}

public record LogMessage
{
    public required string Message { get; init; }
    public required LogLevel Level { get; init; }
    public required string Origin { get; init; }
}
