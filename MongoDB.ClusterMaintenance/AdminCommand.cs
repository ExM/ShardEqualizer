using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public static class AdminCommand
	{
		public static BsonDocument MoveChunk(CollectionNamespace ns, BsonDocument point, string targetShard)
		{
			return new BsonDocument
			{
				{ "moveChunk", ns.FullName },
				{ "find", point },
				{ "to", targetShard }
			};
		}
		
		public static BsonDocument MergeChunks(CollectionNamespace ns, BsonDocument leftBound, BsonDocument rightBound)
		{
			return new BsonDocument
			{
				{ "mergeChunks", ns.FullName },
				{ "bounds", new BsonArray() { leftBound, rightBound }},
			};
		}
	}
}