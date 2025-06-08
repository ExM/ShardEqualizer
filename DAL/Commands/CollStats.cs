using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.DAL.Commands
{
	[BsonIgnoreExtraElements]
	public class CollStats
	{
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
