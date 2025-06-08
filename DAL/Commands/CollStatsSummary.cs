using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.DAL.Commands
{
	[BsonIgnoreExtraElements]
	public class CollStatsSummary
	{
		[BsonElement("primary"), BsonIgnoreIfNull]
		public ShardIdentity? Primary { get; init; }

		[BsonElement("sharded"), BsonRequired]
		public required bool Sharded { get; init; }

		[BsonElement("shards"), BsonDictionaryOptions(DictionaryRepresentation.Document), BsonRequired]
		public required IReadOnlyDictionary<ShardIdentity, CollStats> Shards { get; init; }

		[BsonElement("size"), BsonRequired]
		public required long Size { get; init; }

		[BsonElement("count"), BsonRequired]
		public required long Count { get; init; }

		[BsonElement("storageSize"), BsonRequired]
		public required long StorageSize { get; init; }

		[BsonElement("totalIndexSize"), BsonRequired]
		public required long TotalIndexSize { get; init; }
	}
}
