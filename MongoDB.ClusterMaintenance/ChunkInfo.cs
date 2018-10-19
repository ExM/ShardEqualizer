using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.ClusterMaintenance
{
	public class ChunkInfo
	{
		[BsonId]
		public string Id { get; private set; }

		[BsonElement("lastmodEpoch"), BsonRequired]
		public ObjectId LastmodEpoch { get; private set; }
		
		[BsonElement("lastmod"), BsonRequired]
		public BsonTimestamp Lastmod { get; private set; }
		
		[BsonElement("ns"), BsonRequired]
		public string Namespace { get; private set; }
		
		[BsonElement("min"), BsonRequired]
		public BsonDocument Min { get; private set; }
		
		[BsonElement("max"), BsonRequired]
		public BsonDocument Max { get; private set; }
		
		[BsonElement("shard"), BsonRequired]
		public string Shard { get; private set; }
		
		[BsonElement("jumbo"), BsonIgnoreIfNull]
		public bool? Jumbo { get; private set; }
	}
}