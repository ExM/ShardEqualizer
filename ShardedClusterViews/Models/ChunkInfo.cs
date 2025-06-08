using MongoDB.Bson.Serialization.Attributes;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.ShardedClusterViews.Models;

[BsonIgnoreExtraElements]
public class ChunkInfo
{
	[BsonElement("min"), BsonRequired]
	public required BsonBound Min { get; init; }

	[BsonElement("max"), BsonRequired]
	public required BsonBound Max { get; init; }

	[BsonElement("shard"), BsonRequired]
	public required ShardIdentity Shard { get; init; }

	[BsonElement("jumbo"), BsonIgnoreIfDefault]
	public required bool Jumbo { get; init; }
}