using MongoDB.Bson.Serialization;
using MongoDB.ClusterMaintenance.Serialization;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ConfigDbRepositoryProvider
	{
		private readonly IMongoClient _client;
		private IMongoDatabase _db;

		static ConfigDbRepositoryProvider()
		{
			BsonSerializer.RegisterSerializer(typeof(CollectionNamespace), new CollectionNamespaceSerializer());
		}

		public ConfigDbRepositoryProvider(IMongoClient client)
		{
			_client = client;
			_db = _client.GetDatabase("config");
			
			Chunks = new ChunkRepository(_db);
			Collections = new CollectionRepository(_db);
		}

		public ChunkRepository Chunks { get; }
		
		public CollectionRepository Collections { get; }
	}
}