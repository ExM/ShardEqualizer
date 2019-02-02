using MongoDB.Bson.Serialization;
using MongoDB.ClusterMaintenance.Serialization;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ConfigDbRepositoryProvider : IConfigDbRepositoryProvider
	{
		public ConfigDbRepositoryProvider(IMongoClient client)
		{
			var db = client.GetDatabase("config");
			
			Chunks = new ChunkRepository(db);
			Collections = new CollectionRepository(db);
			Tags = new TagRangeRepository(db);
			Shards = new ShardRepository(db);
		}

		public ChunkRepository Chunks { get; }
		
		public CollectionRepository Collections { get; }
		
		public TagRangeRepository Tags { get; }
		
		public ShardRepository Shards { get; }
	}
}