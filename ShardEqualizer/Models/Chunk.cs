using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ShardEqualizer.Models
{
	public class Chunk
	{
		[BsonId]
		public BsonValue Id { get; set; }

		[BsonElement("lastmodEpoch"), BsonRequired]
		public ObjectId LastmodEpoch { get; set; }

		[BsonElement("lastmod"), BsonRequired]
		public BsonTimestamp Lastmod { get; set; }

		[BsonElement("ns"), BsonRequired]
		public CollectionNamespace Namespace { get; set; }

		[BsonElement("min"), BsonRequired]
		public BsonBound Min { get; set; }

		[BsonElement("max"), BsonRequired]
		public BsonBound Max { get; set; }

		[BsonElement("shard"), BsonRequired]
		public ShardIdentity Shard { get; set; }

		[BsonElement("jumbo"), BsonIgnoreIfDefault]
		public bool Jumbo { get; set; }

		[BsonElement("history"), BsonIgnoreIfNull] //BsonIgnoreIfNull - required for backward compatibility with MongoDB v3.6
		public IReadOnlyList<HistoryEntry> History { get; set; }

		public class HistoryEntry
		{
			[BsonElement("shard"), BsonRequired]
			public ShardIdentity Shard { get; set; }

			[BsonElement("validAfter"), BsonRequired]
			public BsonTimestamp ValidAfter { get; set; }
		}
	}
}
