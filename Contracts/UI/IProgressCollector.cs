namespace ShardEqualizer.Contracts.UI;

public interface IProgressCollector
{
    IProgressReporter Start(string title, long total = 0, Func<long, string>? valueRenderer = null);
    void WriteLine(string line = "");
}