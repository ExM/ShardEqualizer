using ShardEqualizer.DAL.Commands;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.ShardedClusterViews.Models;

public static class MapExtensions
{
    public static ShardCollectionStatistics ToShardCollectionStatistics(this CollStats collStats)
    {
        return new ShardCollectionStatistics()
        {
            Size = collStats.Size,
            StorageSize = collStats.StorageSize,
            TotalIndexSize = collStats.TotalIndexSize
        };
    }
    
    public static CollectionStatistics ToCollectionStatistics(this CollStatsSummary collStat)
    {
        return new CollectionStatistics()
        {
            Primary = collStat.Primary,
            Sharded = collStat.Sharded,
            Shards = collStat.Shards
                .Select(p => new KeyValuePair<ShardIdentity, ShardCollectionStatistics>(p.Key, p.Value.ToShardCollectionStatistics()))
                .ToDictionary(p => p.Key, p => p.Value),

            Size = collStat.Size,
            StorageSize = collStat.StorageSize,
            TotalIndexSize = collStat.TotalIndexSize
        };
    }
    
    public static ChunkInfo ToChunkInfo(this Chunk chunk)
    {
        return new ChunkInfo()
        {
            Min = chunk.Min,
            Max = chunk.Max,
            Shard = chunk.Shard,
            Jumbo = chunk.Jumbo
        };
    }
    
}