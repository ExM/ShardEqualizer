namespace ShardEqualizer.Caching;

public interface IBsonCache<T>
{
	Task<T> Get(CancellationToken token);
}

public interface IBsonCache<T, in TArg0>
{
	Task<T> Get(TArg0 arg0, CancellationToken token);
}