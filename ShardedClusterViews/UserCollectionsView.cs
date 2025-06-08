using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ShardEqualizer.Caching;
using ShardEqualizer.Contracts.UI;

namespace ShardEqualizer.ShardedClusterViews;

public class UserCollectionsView
{
	private readonly ILazyServiceProvider _serviceProvider;
	private readonly IProgressCollector _progressRenderer;
	private readonly IBsonCache<Container> _cache;

	public UserCollectionsView(
		ILazyServiceProvider serviceProvider,
		IProgressCollector progressRenderer,
		IBsonCacheFactory cacheFactory)
	{
		_serviceProvider = serviceProvider;
		_progressRenderer = progressRenderer;

		_cache = cacheFactory.Get(["userCollections"], UploadData);
	}

	public async Task<IReadOnlyCollection<CollectionNamespace>> Get(CancellationToken token)
	{
		return (await _cache.Get(token)).AllUserCollections;
	}

	private async Task<Container> UploadData(CancellationToken token)
	{
		var mongoClient = _serviceProvider.Resolve<IMongoClient>();
		
		await using var reporter = _progressRenderer.Start("Load user collections");

		var allDatabaseNames = await (await mongoClient.ListDatabaseNamesAsync(token)).ToListAsync(token);
		var userDatabases = allDatabaseNames.Except(["admin", "config"]).Select(d => new DatabaseNamespace(d)).ToList();

		reporter.UpdateTotal(userDatabases.Count);
		_progressRenderer.WriteLine($"Found {userDatabases.Count} user databases.");

		async Task<IEnumerable<CollectionNamespace>> ListCollectionNames(DatabaseNamespace dbName,
			CancellationToken t)
		{
			var db = mongoClient.GetDatabase(dbName.DatabaseName);
			var collNames = await (await db.ListCollectionNamesAsync(null, t)).ToListAsync(t);
			
			reporter.Increment();
			return collNames
				.Except(["system.profile"])
				.Select(n => new CollectionNamespace(dbName, n))
				.ToList();
		}

		var results = (await userDatabases.ParallelsAsync(ListCollectionNames, 32, token))
			.SelectMany(nss => nss).ToList();

		reporter.SetCompleteMessage($"found {results.Count} collections.");

		return new Container()
		{
			AllUserCollections = results
		};
	}

	private class Container
	{
		[BsonElement("allUserCollections"), BsonRequired]
		public required IReadOnlyCollection<CollectionNamespace> AllUserCollections { get; init; }
	}
}