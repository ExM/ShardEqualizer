using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.Models
{
	public class TagRangeIdentity
	{
		private TagRangeIdentity()
		{
		}
		
		public TagRangeIdentity(CollectionNamespace ns, BsonDocument min)
		{
			Namespace = ns;
			Min = min;
		}
		
		[BsonElement("ns"), BsonRequired]
		public CollectionNamespace Namespace { get; private set; }
		
		[BsonElement("min"), BsonRequired]
		public BsonDocument Min { get; private set; }
	}
}