using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.ShardedClusterViews.Models;

[BsonIgnoreExtraElements]
public class CollectionStatistics
{
	[BsonElement("primary"), BsonIgnoreIfNull]
	public required ShardIdentity? Primary { get; init; }

	[BsonElement("sharded"), BsonRequired]
	public required bool Sharded { get; init; }

	[BsonElement("shards"), BsonDictionaryOptions(DictionaryRepresentation.Document), BsonIgnoreIfNull]
	public required IReadOnlyDictionary<ShardIdentity, ShardCollectionStatistics> Shards { get; init; }
	
	[BsonElement("size"), BsonRequired]
	public required long Size { get; init; }

	[BsonElement("storageSize"), BsonRequired]
	public required long StorageSize { get; init; }

	[BsonElement("totalIndexSize"), BsonRequired]
	public required long TotalIndexSize { get; init; }
}