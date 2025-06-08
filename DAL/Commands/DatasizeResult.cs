using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.DAL.Commands
{
	[BsonIgnoreExtraElements]
	public class DatasizeResult
	{
		[BsonElement("size"), BsonRequired]
		public required long Size { get; init; }
		
		[BsonElement("numObjects"), BsonRequired]
		public required long NumObjects { get; init; }
	}
}
