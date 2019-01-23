using MongoDB.Bson.Serialization;
using MongoDB.ClusterMaintenance.Serialization;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ConfigDbRepositoryProvider : IConfigDbRepositoryProvider
	{
		static ConfigDbRepositoryProvider()
		{
			BsonSerializer.RegisterSerializer(typeof(CollectionNamespace), new CollectionNamespaceSerializer());
		}

		public ConfigDbRepositoryProvider(IMongoClient client)
		{
			var db = client.GetDatabase("config");
			
			Chunks = new ChunkRepository(db);
			Collections = new CollectionRepository(db);
		}

		public ChunkRepository Chunks { get; }
		
		public CollectionRepository Collections { get; }
	}
}