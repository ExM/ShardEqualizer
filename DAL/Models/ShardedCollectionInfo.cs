using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ShardEqualizer.DAL.Models;

[BsonIgnoreExtraElements]
public class ShardedCollectionInfo
{
	[BsonId]
	public required CollectionNamespace Id { get; init; }

	[BsonElement("uuid"), BsonRequired]
	public required Guid Uuid { get; init; }

	[BsonElement("key"), BsonRequired]
	public required BsonDocument Key { get; init; }

	[BsonElement("unique"), BsonRequired]
	public required bool Unique { get; init; }

	[BsonElement("noBalance"), BsonIgnoreIfDefault]
	public required bool NoBalance { get; init; }

	[BsonElement("maxChunkSizeBytes"), BsonIgnoreIfNull]
	public required long? MaxChunkSizeBytes { get; init; }
}