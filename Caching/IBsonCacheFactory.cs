namespace ShardEqualizer.Caching;

public interface IBsonCacheFactory
{
    IBsonCache<TResult> Get<TResult>(
        string[] relativePath,
        Func<CancellationToken, Task<TResult>> dataUploader);

    IBsonCache<TResult, TArg0> Get<TResult, TArg0>(
        Func<TArg0, string[]> relativePathCreator,
        Func<TArg0, CancellationToken, Task<TResult>> dataUploader);
}