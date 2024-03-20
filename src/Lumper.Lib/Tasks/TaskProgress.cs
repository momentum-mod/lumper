namespace Lumper.Lib.Tasks;
public class TaskProgress
{
    public double Percent { get; private set; }


    private long _Count;
    public long Count
    {
        get => _Count;
        internal set
        {
            _Count = value;
            UpdatePercent();
        }
    }
    private long _Max = -1;
    public long Max
    {
        get => _Max;
        internal set
        {
            _Max = value;
            UpdatePercent();
        }
    }
    public delegate void PercentChangedHandler(object source, double newPercent);
    public event PercentChangedHandler OnPercentChanged;

    public TaskProgress()
    { }
    public TaskProgress(long max) => Max = max;

    private void UpdatePercent()
    {
        Percent = (Count >= 0 && Max > 0)
            ? (double)Count / Max * 100
            : 0;
        if (OnPercentChanged is not null)
            OnPercentChanged(this, Percent);
    }

    public void Reset()
    {
        Count = 0;
        UpdatePercent();
    }

}
