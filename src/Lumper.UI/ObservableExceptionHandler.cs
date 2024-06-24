namespace Lumper.UI;

using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using NLog;
using ReactiveUI;

public class ObservableExceptionHandler : IObserver<Exception>
{
    public void OnNext(Exception value)
    {
        if (Debugger.IsAttached)
            Debugger.Break();

        LogManager.GetCurrentClassLogger().Error(value);

        RxApp.MainThreadScheduler.Schedule(() => throw value);
    }

    public void OnError(Exception error)
    {
        if (Debugger.IsAttached)
            Debugger.Break();

        LogManager.GetCurrentClassLogger().Error(error);

        RxApp.MainThreadScheduler.Schedule(() => throw error);
    }

    public void OnCompleted()
    {
        if (Debugger.IsAttached)
            Debugger.Break();

        RxApp.MainThreadScheduler.Schedule(() => throw new NotImplementedException());
    }
}
