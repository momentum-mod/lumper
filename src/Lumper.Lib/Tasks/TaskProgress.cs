namespace Lumper.Lib.Tasks;

public class TaskProgress
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

    public TaskProgress(long max = -1) => Max = max;

    private void UpdatePercent()
    {
        Percent = Count >= 0 && Max > 0
            ? (double)Count / Max * 100
            : 0;
        OnPercentChanged?.Invoke(this, Percent);
    }

    public void Reset()
    {
        Count = 0;
        UpdatePercent();
    }
}
