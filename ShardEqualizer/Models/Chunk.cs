using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.Models
{
	[BsonIgnoreExtraElements]
	public class Chunk
	{
		[BsonId]
		public BsonValue Id { get; set; }

		[BsonElement("uuid"), BsonRequired, BsonGuidRepresentation(GuidRepresentation.Standard)]
		public Guid Uuid { get; set; }

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
