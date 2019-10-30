using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public static class MongoDatabaseExtensions
	{
		public static async Task<IList<CollectionNamespace>> ListUserCollections(this IMongoDatabase database, CancellationToken token)
		{
			var databaseName = database.DatabaseNamespace.DatabaseName;
			var collNames = await database.ListCollectionNames().ToListAsync(token);
			return collNames
				.Except(_systemCollections)
				.Select(_ => new CollectionNamespace(databaseName, _))
				.ToList();
		}

		private static readonly string[] _systemCollections = new[] {"system.profile"};
	}
}