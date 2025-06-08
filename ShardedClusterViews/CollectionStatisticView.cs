using MongoDB.Driver;
using ShardEqualizer.Caching;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Commands;
using ShardEqualizer.ShardedClusterViews.Models;

namespace ShardEqualizer.ShardedClusterViews;

public class CollectionStatisticView
{
	private readonly ILazyServiceProvider _serviceProvider;
	private readonly IProgressCollector _progressRenderer;
	private readonly IBsonCache<CollectionStatistics, CollectionNamespace> _cache;

	public CollectionStatisticView(
		ILazyServiceProvider serviceProvider,
		IProgressCollector progressRenderer,
		IBsonCacheFactory cacheFactory)
	{
		_serviceProvider = serviceProvider;
		_progressRenderer = progressRenderer;

		_cache = cacheFactory.Get<CollectionStatistics, CollectionNamespace>(
			ns => ["collStats", ns.FullName], UploadData);
	}

	public async Task<IReadOnlyDictionary<CollectionNamespace, CollectionStatistics>> Get(IEnumerable<CollectionNamespace> nss, CancellationToken token)
	{
		var nsList = nss.ToList();
		await using var reporter = _progressRenderer.Start($"Load collection statistics", nsList.Count);

		async Task<(CollectionNamespace ns, CollectionStatistics collStat)> GetCollStat(CollectionNamespace ns, CancellationToken t)
		{
			var result = await _cache.Get(ns, t);
			reporter.Increment();
			return (ns, result);
		}

		var results = await nsList.ParallelsAsync(GetCollStat, 32, token);
		return results.ToDictionary(t => t.ns, t => t.collStat);
	}

	private async Task<CollectionStatistics> UploadData(CollectionNamespace ns, CancellationToken token)
	{
		var mongoClient = _serviceProvider.Resolve<IMongoClient>();
		var db = mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
		var collStat = await db.CollStats(ns.CollectionName, 1, token);
		return collStat.ToCollectionStatistics();
	}
}