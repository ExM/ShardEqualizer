namespace ShardEqualizer.Caching;

public class BsonCache<TResult> : BaseBsonCache<TResult>, IBsonCache<TResult>
{
    private readonly string[] _relativePath;
    private readonly Func<CancellationToken, Task<TResult>> _dataUploader;

    public BsonCache(BsonCacheSettings settings, string[] relativePath, Func<CancellationToken, Task<TResult>> dataUploader): base(settings)
    {
        _relativePath = relativePath;
        _dataUploader = dataUploader;
    }
    
    public Task<TResult> Get(CancellationToken token) =>
        Get(_relativePath, () => _dataUploader(token));
}

public class BsonCache<TResult, TArg0> : BaseBsonCache<TResult>, IBsonCache<TResult, TArg0>
{
    private readonly Func<TArg0, string[]> _relativePathCreator;
    private readonly Func<TArg0, CancellationToken, Task<TResult>> _dataUploader;

    public BsonCache(BsonCacheSettings settings, Func<TArg0, string[]> relativePathCreator, Func<TArg0, CancellationToken, Task<TResult>> dataUploader): base(settings)
    {
        _relativePathCreator = relativePathCreator;
        _dataUploader = dataUploader;
    }
    
    public Task<TResult> Get(TArg0 arg0, CancellationToken token) => 
        Get(_relativePathCreator(arg0), () => _dataUploader(arg0, token));
}