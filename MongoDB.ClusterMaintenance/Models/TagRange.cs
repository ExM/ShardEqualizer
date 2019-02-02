using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.Models
{
	public class TagRange
	{
		private TagRange()
		{
		}

		public TagRange(string tagName, BsonDocument min, BsonDocument max)
		{
			Tag = new TagIdentity(tagName);
			Min = min;
			Max = max;
			Namespace = new CollectionNamespace("test", "test");
			Id = new TagRangeIdentity(Namespace, min);
		}
		
		[BsonId]
		public TagRangeIdentity Id { get; private set; }

		[BsonElement("ns"), BsonRequired]
		public CollectionNamespace Namespace { get; private set; }
		
		[BsonElement("min"), BsonRequired]
		public BsonDocument Min { get; private set; }
		
		[BsonElement("max"), BsonRequired]
		public BsonDocument Max { get; private set; }
		
		[BsonElement("tag"), BsonRequired]
		public TagIdentity Tag { get; private set; }
	}
}