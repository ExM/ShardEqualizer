using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ShardEqualizer.LocalStoring;

namespace ShardEqualizer
{
	public class CollectionListService
	{
		private readonly IMongoClient _mongoClient;
		private readonly ProgressRenderer _progressRenderer;
		private readonly ILocalStore<Container> _store;

		public CollectionListService(
			IMongoClient mongoClient,
			ProgressRenderer progressRenderer,
			LocalStoreProvider storeProvider)
		{
			_mongoClient = mongoClient;
			_progressRenderer = progressRenderer;
			_store = storeProvider.Get("userCollections", uploadData);
		}

		public async Task<IReadOnlyCollection<CollectionNamespace>> Get(CancellationToken token)
		{
			var result = await _store.Get(token);
			return result.AllUserCollections;
		}

		private async Task<Container> uploadData(CancellationToken token)
		{
			await using var reporter = _progressRenderer.Start("Load user collections");

			var userDatabases = await _mongoClient.ListUserDatabases(token);
			reporter.UpdateTotal(userDatabases.Count);
			_progressRenderer.WriteLine($"Found {userDatabases.Count} user databases.");


			async Task<IEnumerable<CollectionNamespace>> listCollectionNames(DatabaseNamespace dbName,
				CancellationToken t)
			{
				var colls = await _mongoClient.GetDatabase(dbName.DatabaseName).ListUserCollections(t);
				reporter.Increment();
				return colls;
			}

			var results = (await userDatabases.ParallelsAsync(listCollectionNames, 32, token))
				.SelectMany(_ => _).ToList();

			reporter.SetCompleteMessage($"found {results.Count} collections.");

			return new Container()
			{
				AllUserCollections = results
			};
		}

		private class Container
		{
			[BsonElement("allUserCollections"), BsonRequired]
			public IReadOnlyCollection<CollectionNamespace> AllUserCollections { get; set; }
		}
	}
}
