using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ShardEqualizer.Caching;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.Repositories;

namespace ShardEqualizer.ShardedClusterViews;

public class TagRangesView
{
	private readonly ILazyServiceProvider _serviceProvider;
	private readonly IProgressCollector _progressRenderer;
	private readonly IBsonCache<Container, CollectionNamespace> _cache;

	public TagRangesView(
		ILazyServiceProvider serviceProvider,
		IProgressCollector progressRenderer,
		IBsonCacheFactory cacheFactory)
	{
		_serviceProvider = serviceProvider;
		_progressRenderer = progressRenderer;

		_cache = cacheFactory.Get<Container, CollectionNamespace>(
			ns => ["tagRanges", ns.FullName], UploadTagRanges);
	}

	public async Task<IReadOnlyDictionary<CollectionNamespace, IReadOnlyList<TagRange>>> Get(IEnumerable<CollectionNamespace> nss, CancellationToken token)
	{
		var nsList = nss.ToList();

		await using var reporter = _progressRenderer.Start($"Load tag ranges", nsList.Count);

		async Task<(CollectionNamespace ns, IReadOnlyList<TagRange> tagRanges)> GetTagRanges(CollectionNamespace ns, CancellationToken t)
		{
			var container = await _cache.Get(ns, t);
			reporter.Increment();
			return (ns, container.TagRanges);
		}

		var pairs = await nsList.ParallelsAsync(GetTagRanges, 32, token);

		return pairs.ToDictionary(p => p.ns, p => p.tagRanges);
	}

	private async Task<Container> UploadTagRanges(CollectionNamespace ns, CancellationToken token)
	{
		var repo = _serviceProvider.Resolve<TagRangeRepository>();
		return new Container()
		{
			TagRanges = await repo.Get(ns, token)
		};
	}

	private class Container
	{
		[BsonElement("tagRanges")]
		public required IReadOnlyList<TagRange> TagRanges { get; init; }
	}
}