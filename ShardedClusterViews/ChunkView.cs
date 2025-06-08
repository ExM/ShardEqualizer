using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ShardEqualizer.Caching;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Repositories;
using ShardEqualizer.ShardedClusterViews.ChunkCaching;
using ShardEqualizer.ShardedClusterViews.Models;

namespace ShardEqualizer.ShardedClusterViews;

public class ChunkView
{
	private readonly ILazyServiceProvider _serviceProvider;
	private readonly ShardedCollectionInfoView _shardedCollectionInfoView;
	private readonly IProgressCollector _progressRenderer;
	private readonly IBsonCache<Container, CollectionNamespace> _cache;

	public ChunkView(
		ILazyServiceProvider serviceProvider,
		ShardedCollectionInfoView shardedCollectionInfoView,
		IProgressCollector progressRenderer,
		IBsonCacheFactory cacheFactory)
	{
		_serviceProvider = serviceProvider;
		_shardedCollectionInfoView = shardedCollectionInfoView;
		_progressRenderer = progressRenderer;

		_cache = cacheFactory.Get<Container, CollectionNamespace>(ns => ["chunks", ns.FullName], UploadData);
	}

	public async Task<IReadOnlyDictionary<CollectionNamespace, ChunksCache>> Get(IEnumerable<CollectionNamespace> nss, CancellationToken token)
	{
		var nsList = nss.ToList();

		await using var reporter = _progressRenderer.Start($"Load chunks", nsList.Count);
		
		async Task<(CollectionNamespace ns, List<ChunkInfo> chunks)> GetChunks(CollectionNamespace ns, CancellationToken t)
		{
			var container = await _cache.Get(ns, t);
			reporter.Increment();
			return (ns, container.Chunks);
		}

		var pairs = await nsList.ParallelsAsync(GetChunks, 32, token);

		return pairs.ToDictionary(p => p.ns, p => new ChunksCache(p.chunks));
	}

	private async Task<Container> UploadData(CollectionNamespace ns, CancellationToken t)
	{
		var repo = _serviceProvider.Resolve<ChunkRepository>();
		
		var collection = await _shardedCollectionInfoView.Get(ns, t);
			
		var expectedCount = await repo.ByUuid(collection.Uuid).Count(t);
		var chunks = new List<ChunkInfo>((int)expectedCount);
		using var cursor = await repo.ByUuid(collection.Uuid).Find(t);
		while (await cursor.MoveNextAsync(t))
			chunks.AddRange(cursor.Current.Select(c => c.ToChunkInfo()));

		return new Container(){ Chunks = chunks };
	}

	private class Container
	{
		[BsonElement("chunks")]
		public required List<ChunkInfo> Chunks { get; init; }
	}
}