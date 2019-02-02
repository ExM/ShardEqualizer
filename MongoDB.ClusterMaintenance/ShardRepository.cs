using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ShardRepository
	{
		private readonly IMongoCollection<Shard> _coll;
		
		public ShardRepository(IMongoDatabase db)
		{
			_coll = db.GetCollection<Shard>("shards");
		}
		
		public async Task<IReadOnlyCollection<Shard>> GetAll()
		{
			return await _coll.Find(Builders<Shard>.Filter.Empty).ToListAsync();
		}
	}
}