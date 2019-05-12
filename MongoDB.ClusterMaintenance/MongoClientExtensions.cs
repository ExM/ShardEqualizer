using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public static class MongoClientExtensions
	{
		public static async Task<IList<CollectionNamespace>> ListUserCollections(this IMongoClient mongoClient, CancellationToken token)
		{
			var allDatabaseNames = await mongoClient.ListDatabaseNames().ToListAsync(token);
			var userDataBases = allDatabaseNames.Except(new[] {"admin", "config"}).ToList();
			
			async Task<IEnumerable<CollectionNamespace>> listCollectionNames(string dbName, CancellationToken t)
			{
				var db = mongoClient.GetDatabase(dbName);
				var collNames = await db.ListCollectionNames().ToListAsync(t);
				return collNames.Select(_ => new CollectionNamespace(dbName, _));
			}
			
			return (await userDataBases.ParallelsAsync(listCollectionNames, 8, token)).SelectMany(_ => _).ToList();
		}
	}
}