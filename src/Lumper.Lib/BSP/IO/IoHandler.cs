namespace Lumper.Lib.BSP.IO;

using System;
using System.Threading;

/// <summary>
/// Utility class that can be passed around during save/load to handle progress and cancellation.
///
/// Not being overly fancy with progress, doesn't need to be perfectly accurate.
/// Important thing is that the user is shown ongoing progress as slow operations
/// like pakfile compression are ongoing.
/// </summary>
public class IoHandler(CancellationTokenSource cts)
{
    public CancellationTokenSource CancellationTokenSource { get; } = cts;

    // Just assign a single token for everything to use
    private CancellationToken CancellationToken { get; } = cts.Token;

    public bool Cancelled => CancellationToken.IsCancellationRequested;

    public event EventHandler<IoProgressEventArgs>? Progress;

    public float ProgressPercent { get; set; }

    public void UpdateProgress(float progressIncr, string? message = null)
    {
        ProgressPercent += progressIncr;
        Progress?.Invoke(this, new IoProgressEventArgs { ProgressPercent = ProgressPercent, Message = message });
    }

    public void UpdateAbsProgress(float absProgress)
    {
        ProgressPercent = absProgress;
        Progress?.Invoke(this, new IoProgressEventArgs { ProgressPercent = absProgress, Message = null });
    }

    public enum ReadProgressProportions
    {
        Header = 5,
        OtherLumps = 50,
        Paklump = 45
    }

    public enum WriteProgressProportions
    {
        Header = 5,
        OtherLumps = 25,
        Paklump = 70
    }
}

public class IoProgressEventArgs : EventArgs
{
    public required float ProgressPercent { get; init; }
    public string? Message { get; init; }
}
