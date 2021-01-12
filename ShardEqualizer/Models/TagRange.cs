using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ShardEqualizer.Models
{
	public class TagRange
	{
		private TagRange()
		{
		}

		public TagRange(string tagName, BsonBound min, BsonBound max)
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
		public BsonBound Min { get; private set; }
		
		[BsonElement("max"), BsonRequired]
		public BsonBound Max { get; private set; }
		
		[BsonElement("tag"), BsonRequired]
		public TagIdentity Tag { get; private set; }
	}
}