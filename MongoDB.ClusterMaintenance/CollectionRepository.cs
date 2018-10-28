using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class CollectionRepository
	{
		private IMongoCollection<ShardedCollectionInfo> _coll;

		public CollectionRepository(IMongoClient client)
		{
			_coll = client
				.GetDatabase("config")
				.GetCollection<ShardedCollectionInfo>("collections");
		}

		public Task<ShardedCollectionInfo> Find(CollectionNamespace ns)
		{
			return _coll.Find(_ => _.Id == ns.FullName).FirstOrDefaultAsync();
		}
	}
}