using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ShardEqualizer.Models
{
	public class TagRangeIdentity
	{
		private TagRangeIdentity()
		{
		}
		
		public TagRangeIdentity(CollectionNamespace ns, BsonBound min)
		{
			Namespace = ns;
			Min = min;
		}
		
		[BsonElement("ns"), BsonRequired]
		public CollectionNamespace Namespace { get; private set; }
		
		[BsonElement("min"), BsonRequired]
		public BsonBound Min { get; private set; }
	}
}