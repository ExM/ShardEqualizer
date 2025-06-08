using MongoDB.Bson.Serialization.Attributes;
using ShardEqualizer.Caching;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.Repositories;

namespace ShardEqualizer.ShardedClusterViews;

public class ShardsView
{
	private readonly ILazyServiceProvider _serviceProvider;
	private readonly IProgressCollector _progressRenderer;
	private readonly IBsonCache<Container> _cache;

	public ShardsView(
		ILazyServiceProvider serviceProvider,
		IProgressCollector progressRenderer,
		IBsonCacheFactory cacheFactory)
	{
		_serviceProvider = serviceProvider;
		_progressRenderer = progressRenderer;
		_cache = cacheFactory.Get(["shards"], UploadData);
	}

	public async Task<IReadOnlyCollection<Shard>> Get(CancellationToken token)
	{
		return (await _cache.Get(token)).Shards;
	}

	private async Task<Container> UploadData(CancellationToken token)
	{
		var repo = _serviceProvider.Resolve<ShardRepository>();
		await using var reporter = _progressRenderer.Start("Load shard list");
		var result = await repo.GetAll(token);
		reporter.SetCompleteMessage($"found {result.Count} shards.");

		return new Container()
		{
			Shards = result
		};
	}

	private class Container
	{
		[BsonElement("shards"), BsonRequired]
		public required IReadOnlyCollection<Shard> Shards { get; init; }
	}
}