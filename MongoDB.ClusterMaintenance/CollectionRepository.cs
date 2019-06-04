using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class CollectionRepository
	{
		private IMongoCollection<ShardedCollectionInfo> _coll;

		internal CollectionRepository(IMongoDatabase db)
		{
			_coll = db.GetCollection<ShardedCollectionInfo>("collections");
		}

		public Task<ShardedCollectionInfo> Find(CollectionNamespace ns)
		{
			return _coll.Find(_ => _.Id == ns).FirstOrDefaultAsync();
		}
		
		public async Task<IReadOnlyList<ShardedCollectionInfo>> FindAll(bool includeConfigCollections = false)
		{
			var result = await _coll.Find(Builders<ShardedCollectionInfo>.Filter.Empty).ToListAsync();
			if (!includeConfigCollections)
				result.RemoveAll(_ => string.Equals(_.Id.DatabaseNamespace.DatabaseName, "config", StringComparison.Ordinal));

			return result;
		}
	}
}