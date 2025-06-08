using System.Collections;
using System.Collections.Concurrent;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ShardEqualizer.Caching;

public class BaseBsonCache<T>
{
    private readonly BsonCacheSettings _settings;

    private readonly ConcurrentDictionary<string[], Lazy<Task<T>>> _cache = new (ArrayComparer.Instance);

    private readonly Func<string[], Func<Task<T>>, Lazy<Task<T>>> _cacheEntryFactory;

    protected BaseBsonCache(BsonCacheSettings settings)
    {
        _settings = settings;
        _cacheEntryFactory = ValueFactory;
    }
    
    protected Task<T> Get(string[] relativePath, Func<Task<T>> uploadData)
    {
        return _cache.GetOrAdd(relativePath, _cacheEntryFactory, uploadData).Value;
    }

    private Lazy<Task<T>> ValueFactory(string[] relativePath, Func<Task<T>> uploadData)
    {
        return new Lazy<Task<T>>(() => UploadData(relativePath, uploadData));
    }

    private async Task<T> UploadData(string[] relativePath, Func<Task<T>> uploadData)
    {
        var fullPath = new List<string>(relativePath.Length + 1);
        fullPath.Add(_settings.BasePath);
        fullPath.AddRange(relativePath);
        fullPath[^1] += ".bson";

        var fileName = Path.Combine(fullPath.ToArray());
        
        if (_settings.AllowRead && File.Exists(fileName))
        {
            await using var stream = File.OpenRead(fileName);
            return BsonSerializer.Deserialize<T>(stream); //TODO async read to memory
        }

        var data = await uploadData();

        if (_settings.AllowWrite)
        {
            var path = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path!);
            
            await using var stream = File.Open(fileName, FileMode.Create);
            using var bsonWriter = new BsonBinaryWriter(stream); //TODO write to memory, async write to file
            BsonSerializer.Serialize(bsonWriter, data);
            bsonWriter.Flush();
            stream.Flush();
        }

        return data;
    }

    sealed class ArrayComparer : EqualityComparer<string[]>
    {
        public static readonly ArrayComparer Instance = new ();
        
        public override bool Equals(string[]? x, string[]? y)
            => StructuralComparisons.StructuralEqualityComparer.Equals(x, y);

        public override int GetHashCode(string[] x)
            => StructuralComparisons.StructuralEqualityComparer.GetHashCode(x);
    }
}