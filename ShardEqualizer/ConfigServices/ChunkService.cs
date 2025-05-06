using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ShardEqualizer.ChunkCaching;
using ShardEqualizer.ConfigRepositories;
using ShardEqualizer.LocalStoring;
using ShardEqualizer.ShortModels;
using ShardEqualizer.UI;

namespace ShardEqualizer.ConfigServices
{
	public class ChunkService
	{
		private readonly ChunkRepository _repo;
		private readonly ProgressRenderer _progressRenderer;
		private readonly INsLocalStore<Container> _store;
		private readonly ShardedCollectionService _shardedCollectionService;

		public ChunkService(
			ChunkRepository repo,
			ProgressRenderer progressRenderer,
			LocalStoreProvider storeProvider, ShardedCollectionService shardedCollectionService)
		{
			_repo = repo;
			_progressRenderer = progressRenderer;
			_shardedCollectionService = shardedCollectionService;
			_store = storeProvider.Get("chunks", uploadChunks);
		}

		public async Task<IReadOnlyDictionary<CollectionNamespace, ChunksCache>> Get(IEnumerable<CollectionNamespace> ns, CancellationToken token)
		{
			var nsList = ns.ToList();

			await using var reporter = _progressRenderer.Start($"Load chunks", nsList.Count);
			{
				async Task<(CollectionNamespace ns, List<ChunkInfo> chunks)> getChunks(CollectionNamespace ns, CancellationToken t)
				{
					var container = await _store.Get(ns, t);
					reporter.Increment();
					return (ns, container.Chunks);
				}

				var pairs = await nsList.ParallelsAsync(getChunks, 32, token);

				return pairs.ToDictionary(_ => _.ns, _ => new ChunksCache(_.chunks));
			}
		}

		private async Task<Container> uploadChunks(CollectionNamespace ns, CancellationToken t)
		{
			var collection = await _shardedCollectionService.Get(ns, t);

			var expectedCount = await _repo.ByUuid(collection.Uuid).Count(t);
			var chunks = new List<ChunkInfo>((int)expectedCount);
			using var cursor = await _repo.ByUuid(collection.Uuid).Find(t);
			while (await cursor.MoveNextAsync(t))
				chunks.AddRange(cursor.Current.Select(_ => new ChunkInfo(_)));

			return new Container(){ Chunks = chunks };
		}

		private class Container
		{
			[BsonElement("chunks")]
			public List<ChunkInfo> Chunks { get; set; }
		}
	}
}
