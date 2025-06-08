namespace ShardEqualizer.Contracts.UI;

public interface IProgressReporter: IAsyncDisposable
{
    void UpdateTotal(long total);

    public void Increment();

    public void Increment(long value);

    public void SetCompleteMessage(string message);
}