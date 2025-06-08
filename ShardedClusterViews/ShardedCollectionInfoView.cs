using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using ShardEqualizer.Caching;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.Repositories;

namespace ShardEqualizer.ShardedClusterViews;

public class ShardedCollectionInfoView
{
	private readonly ILazyServiceProvider _serviceProvider;
	private readonly IProgressCollector _progressRenderer;
	private readonly IBsonCache<Container> _cache;

	public ShardedCollectionInfoView(
		ILazyServiceProvider serviceProvider,
		IProgressCollector progressRenderer,
		IBsonCacheFactory cacheFactory)
	{
		_serviceProvider = serviceProvider;
		_progressRenderer = progressRenderer;
		_cache = cacheFactory.Get(["shardedCollections"], UploadData);
	}

	public async Task<IReadOnlyDictionary<CollectionNamespace, ShardedCollectionInfo>> Get(CancellationToken token)
	{
		return (await _cache.Get(token)).ShardedCollection;
	}

	public async Task<ShardedCollectionInfo> Get(CollectionNamespace ns, CancellationToken token)
	{
		return (await Get(token)).GetValueOrDefault(ns)
		       ?? throw new InvalidOperationException($"Cannot find collection by namespace '{ns}'");
	}

	private async Task<Container> UploadData(CancellationToken token)
	{
		var repo = _serviceProvider.Resolve<CollectionRepository>();
		await using var reporter = _progressRenderer.Start("Load sharded collections");
		var result = await repo.FindAll(token);

		var message = $"found {result.Count} collections";
		reporter.SetCompleteMessage(message);

		return new Container()
		{
			ShardedCollection = result.ToDictionary(i => i.Id)
		};
	}

	private class Container
	{
		[BsonElement("shardedCollections"), BsonDictionaryOptions(DictionaryRepresentation.Document), BsonIgnoreIfNull]
		public required Dictionary<CollectionNamespace, ShardedCollectionInfo> ShardedCollection { get; init; }
	}
}