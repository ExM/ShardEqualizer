using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.Models
{
	public class Chunk
	{
		[BsonId]
		public string Id { get; set; }

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
	}
}