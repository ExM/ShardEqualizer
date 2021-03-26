using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;
using ShardEqualizer.ChunkCaching;
using ShardEqualizer.LocalStoring;
using ShardEqualizer.ShortModels;

namespace ShardEqualizer
{
	public class ChunkService
	{
		private readonly ChunkRepository _repo;
		private readonly ProgressRenderer _progressRenderer;
		private readonly INsLocalStore<Container> _store;

		public ChunkService(
			ChunkRepository repo,
			ProgressRenderer progressRenderer,
			LocalStoreProvider storeProvider)
		{
			_repo = repo;
			_progressRenderer = progressRenderer;
			_store = storeProvider.Get("chunks", uploadChunks);
		}

		public async Task<IReadOnlyDictionary<CollectionNamespace, ChunksCache>> Get(IEnumerable<CollectionNamespace> nss, CancellationToken token)
		{
			var nsList = nss.ToList();

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
			var expectedCount = await _repo.ByNamespace(ns).Count(t);
			var chunks = new List<ChunkInfo>((int)expectedCount);
			using var cursor = await _repo.ByNamespace(ns).Find(t);
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
