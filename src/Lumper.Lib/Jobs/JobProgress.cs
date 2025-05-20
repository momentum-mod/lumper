namespace Lumper.Lib.Jobs;

public class JobProgress
{
    public double Percent { get; private set; }

    private long _count;
    public long Count
    {
        get => _count;
        internal set
        {
            _count = value;
            UpdatePercent();
        }
    }

    private long _max;
    public long Max
    {
        get => _max;
        internal set
        {
            _max = value;
            UpdatePercent();
        }
    }

    public delegate void PercentChangedHandler(object source, double newPercent);

    public event PercentChangedHandler? OnPercentChanged;

    public JobProgress(long max = -1)
    {
        Max = max;
    }

    private void UpdatePercent()
    {
        if (Count >= 0 && Max > 0)
            Percent = (double)Count / Max * 100;
        else
            Percent = 0;

        OnPercentChanged?.Invoke(this, Percent);
    }

    public void Reset()
    {
        Count = 0;
        UpdatePercent();
    }
}
