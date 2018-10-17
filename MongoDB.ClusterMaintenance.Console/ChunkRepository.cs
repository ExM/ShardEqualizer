using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ChunkRepository
	{
		private IMongoCollection<ChunkInfo> _coll;

		public ChunkRepository(IMongoClient client)
		{
			_coll = client
				.GetDatabase("config")
				.GetCollection<ChunkInfo>("chunks");
		}

		public Task<IAsyncCursor<ChunkInfo>> Find(string ns)
		{
			return _coll.FindAsync(_ => _.Namespace == ns);
		}

		public Task<long> Count(string ns)
		{
			return _coll.CountDocumentsAsync(_ => _.Namespace == ns);
		}
	}
}