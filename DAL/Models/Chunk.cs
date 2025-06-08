using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.DAL.Models;

[BsonIgnoreExtraElements]
public class Chunk
{
	[BsonId]
	public required BsonValue Id { get; init; }

	[BsonElement("uuid"), BsonRequired]
	public required Guid Uuid { get; init; } //UDONE domain type CollectionId

	[BsonElement("min"), BsonRequired]
	public required BsonBound Min { get; init; }

	[BsonElement("max"), BsonRequired]
	public required BsonBound Max { get; init; }

	[BsonElement("shard"), BsonRequired]
	public required ShardIdentity Shard { get; init; }

	[BsonElement("jumbo"), BsonIgnoreIfDefault]
	public bool Jumbo { get; init; }
}