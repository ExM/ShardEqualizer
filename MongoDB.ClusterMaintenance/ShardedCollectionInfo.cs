using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.ClusterMaintenance
{
	public class ShardedCollectionInfo
	{
		[BsonId]
		public string Id { get; private set; }

		[BsonElement("lastmodEpoch"), BsonRequired]
		public ObjectId LastmodEpoch { get; private set; }
		
		[BsonElement("lastmod"), BsonRequired]
		public DateTime Lastmod { get; private set; }
		
		[BsonElement("dropped"), BsonRequired]
		public bool Dropped { get; private set; }
		
		[BsonElement("key"), BsonIgnoreIfNull]
		public BsonDocument Key { get; private set; }
		
		[BsonElement("unique"), BsonIgnoreIfNull]
		public bool? Unique { get; private set; }
		
		[BsonElement("noBalance"), BsonIgnoreIfNull]
		public bool? NoBalance { get; private set; }
	}
}