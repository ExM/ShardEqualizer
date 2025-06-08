using MongoDB.Bson.Serialization.Attributes;
using ShardEqualizer.DAL.Commands;

namespace ShardEqualizer.ShardedClusterViews.Models;

[BsonIgnoreExtraElements]
public class ShardCollectionStatistics
{
	[BsonElement("size"), BsonRequired]
	public required long Size { get; init; }

	[BsonElement("storageSize"), BsonRequired]
	public required long StorageSize { get; init; }

	[BsonElement("totalIndexSize"), BsonRequired]
	public required long TotalIndexSize { get; init; }
}