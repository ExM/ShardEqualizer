namespace ShardEqualizer.Caching;

public class BsonCacheSettings
{
    public required string BasePath { get; init; }
    public required bool AllowRead { get; init; }
    public required bool AllowWrite { get; init; }
}