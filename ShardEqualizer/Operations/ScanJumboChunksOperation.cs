using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NLog;
using ShardEqualizer.ByteSizeRendering;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL.Commands;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.Repositories;
using ShardEqualizer.ShardedClusterViews;

namespace ShardEqualizer.Operations
{
	public class ScanJumboChunksOperation : IOperation
	{
		private readonly IMongoClient _mongoClient;
		private readonly IProgressCollector _progressRenderer;
		private readonly ShardedCollectionInfoView _shardedCollectionInfoView;
		private readonly ChunkRepository _chunkRepo;

		public ScanJumboChunksOperation(
			ShardedCollectionInfoView shardedCollectionInfoView,
			ChunkRepository chunkRepo,
			IMongoClient mongoClient,
			IProgressCollector progressRenderer)
		{
			_mongoClient = mongoClient;
			_progressRenderer = progressRenderer;
			_shardedCollectionInfoView = shardedCollectionInfoView;
			_chunkRepo = chunkRepo;
		}

		private async Task<IReadOnlyDictionary<CollectionNamespace, List<Chunk>>> findJumboChunks(IReadOnlyCollection<CollectionNamespace> namespaces, CancellationToken token)
		{
			await using var reporter = _progressRenderer.Start($"Find jumbo chunks", namespaces.Count);
			{
				async Task<(CollectionNamespace ns, List<Chunk> chunks)> loadCollChunks(CollectionNamespace ns, CancellationToken t)
				{
					var collection = await _shardedCollectionInfoView.Get(ns, token);
					var allChunks = await (await _chunkRepo
						.ByUuid(collection.Uuid)
						.OnlyJumbo()
						.Find(t)).ToListAsync(t);
					reporter.Increment();
					return (ns, allChunks);
				}

				var results = await namespaces.ParallelsAsync(loadCollChunks, 16, token);
				var jumboChunks = results.ToDictionary(x => x.ns, x => x.chunks);
				reporter.SetCompleteMessage($"found {jumboChunks.SelectMany(x => x.Value).Count()} chunks.");
				return jumboChunks;
			}
		}

		private async Task<ICollection<ChunkDataSize>> scanJumboChunks(
			IReadOnlyDictionary<CollectionNamespace, List<Chunk>> jumboChunksInfo,
			IReadOnlyDictionary<CollectionNamespace, ShardedCollectionInfo> collectionsInfo,
			CancellationToken token)
		{
			await using var reporter = _progressRenderer.Start($"Scan jumbo chunks", jumboChunksInfo.SelectMany(x => x.Value).Count());
			{
				async Task<ChunkDataSize> scanChunk((CollectionNamespace ns, Chunk chunk) chunkInfo, CancellationToken t)
				{
					var (ns, chunk) = chunkInfo;

					var db = _mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
					var collInfo = collectionsInfo[ns];

					try
					{
						var result = await db.Datasize(collInfo, chunk, true, t);

						reporter.Increment();

						return new ChunkDataSize(ns, chunk.Shard, result.Size, result.NumObjects);
					}
					catch (MongoCommandException ex)
					{
						_progressRenderer.WriteLine($"chunk {chunk.Id} - datasize command fail { ex.ErrorMessage}"); //TODO write to stderr
						throw;
					}
				}

				var results = await jumboChunksInfo.SelectMany(x => x.Value.Select(y => (x.Key,y))).ToList().ParallelsAsync(scanChunk, 32, token);

				return results.Where(_ => _ != null).ToList();
			}
		}

		private static readonly List<double> _percentiles = new List<double>()
			{ 0, .50, .75, .90, .95, .99, 1};

		private static readonly List<string> _percentileName = new List<string>()
			{ "min", "50", "75", "90", "95", "99", "max"};

		private void renderPercentiles(IEnumerable<long> sizes)
		{
			var renderedValues = sizes.CalcPercentiles(_percentiles).Zip(_percentileName, (size, name) => $"{name}: {size.ByteSize()}");
			Console.WriteLine("   {0}", string.Join(", ", renderedValues));
		}

		private void renderResults(ICollection<ChunkDataSize> chunkDataSizes)
		{
			Console.WriteLine();
			Console.WriteLine("Chunk size percentiles.");
			Console.WriteLine();
			Console.WriteLine("By namespaces:");
			foreach (var nsGroup in chunkDataSizes.GroupBy(_ => _.Ns))
			{
				var group = nsGroup.ToList();
				Console.WriteLine(" * {0} - count: {1}, empty: {2}, size: {3}", nsGroup.Key, group.Count, group.Count(_ => _.Size == 0), group.Sum(_ => _.Size).ByteSize());
				renderPercentiles(group.Select(_ => _.Size));
			}

			Console.WriteLine();
			Console.WriteLine("By shards:");
			foreach (var shardGroup in chunkDataSizes.GroupBy(_ => _.Shard))
			{
				var group = shardGroup.ToList();
				Console.WriteLine(" * {0} - count: {1}, empty: {2}, size: {3}", shardGroup.Key, group.Count, group.Count(_ => _.Size == 0), group.Sum(_ => _.Size).ByteSize());
				renderPercentiles(group.Select(_ => _.Size));
			}

			Console.WriteLine();
			Console.WriteLine("Total - count: {0}, empty: {1}, size: {2}", chunkDataSizes.Count, chunkDataSizes.Count(_ => _.Size == 0), chunkDataSizes.Sum(_ => _.Size).ByteSize());
			renderPercentiles(chunkDataSizes.Select(_ => _.Size));
		}

		public async Task Run(CancellationToken token)
		{
			var collectionsInfo = await _shardedCollectionInfoView.Get(token);
			var shardedNamespaces = collectionsInfo.Values.Select(_ => _.Id).ToList();

			var jumboChunks = await findJumboChunks(shardedNamespaces, token);
			var chunkDataSizes = await scanJumboChunks(jumboChunks, collectionsInfo, token);

			renderResults(chunkDataSizes);
		}

		private class ChunkDataSize
		{
			public CollectionNamespace Ns { get; }
			public ShardIdentity Shard { get; }
			public long Size { get; }
			public long NumObjects { get; }

			public ChunkDataSize(CollectionNamespace ns, ShardIdentity shard, long size, long numObjects)
			{
				Ns = ns;
				Shard = shard;
				Size = size;
				NumObjects = numObjects;
			}
		}

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
	}
}
