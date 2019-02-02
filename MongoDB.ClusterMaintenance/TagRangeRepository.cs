using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class TagRangeRepository
	{
		private readonly IMongoCollection<TagRange> _coll;
		
		public TagRangeRepository(IMongoDatabase db)
		{
			_coll = db.GetCollection<TagRange>("tags");
		}
		
		public async Task<IReadOnlyList<TagRange>> Get(CollectionNamespace ns)
		{
			return await _coll
				.Find(_ => _.Namespace == ns)
				.Sort(Builders<TagRange>.Sort.Ascending(_ => _.Id))
				.ToListAsync();
		}
	}
}