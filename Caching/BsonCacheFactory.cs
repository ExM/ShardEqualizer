using ShardEqualizer.Caching.Configs;

namespace ShardEqualizer.Caching;

public class BsonCacheFactory : IBsonCacheFactory
{
    private readonly BsonCacheSettings _settings;

    public BsonCacheFactory(LocalStoreConfig config)
    {
        _settings = new BsonCacheSettings()
        {
            BasePath = Path.GetFullPath(Path.Combine(".", "cache")),
            AllowRead = config.Read == true,
            AllowWrite = config.Write == true
        };

        if (Directory.Exists(_settings.BasePath))
        {
            if (config.Clean == true)
            {
                Directory.Delete(_settings.BasePath, true);
                if(_settings.AllowWrite)
                    Directory.CreateDirectory(_settings.BasePath);
            }
        }
        else if(_settings.AllowWrite)
            Directory.CreateDirectory(_settings.BasePath);
    }
    
    public IBsonCache<TResult> Get<TResult>(string[] relativePath, Func<CancellationToken, Task<TResult>> dataUploader)
    {
        return new BsonCache<TResult>(_settings, relativePath, dataUploader);
    }

    public IBsonCache<TResult, TArg0> Get<TResult, TArg0>(Func<TArg0, string[]> relativePathCreator, Func<TArg0, CancellationToken, Task<TResult>> dataUploader)
    {
        return new BsonCache<TResult, TArg0>(_settings, relativePathCreator, dataUploader);
    }
}