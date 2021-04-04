using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NLog;
using ShardEqualizer.ByteSizeRendering;
using ShardEqualizer.ConfigRepositories;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.Models;
using ShardEqualizer.MongoCommands;
using ShardEqualizer.UI;

namespace ShardEqualizer.Operations
{
	public class ScanJumboChunksOperation : IOperation
	{
		private readonly IMongoClient _mongoClient;
		private readonly ProgressRenderer _progressRenderer;
		private readonly ShardedCollectionService _shardedCollectionService;
		private readonly ChunkRepository _chunkRepo;

		public ScanJumboChunksOperation(
			ShardedCollectionService shardedCollectionService,
			ChunkRepository chunkRepo,
			IMongoClient mongoClient,
			ProgressRenderer progressRenderer)
		{
			_mongoClient = mongoClient;
			_progressRenderer = progressRenderer;
			_shardedCollectionService = shardedCollectionService;
			_chunkRepo = chunkRepo;
		}

		private async Task<List<Chunk>> findJumboChunks(IReadOnlyCollection<CollectionNamespace> namespaces, CancellationToken token)
		{
			await using var reporter = _progressRenderer.Start($"Find jumbo chunks", namespaces.Count);
			{
				async Task<List<Chunk>> loadCollChunks(CollectionNamespace ns, CancellationToken t)
				{
					var allChunks = await (await _chunkRepo
						.ByNamespace(ns)
						.OnlyJumbo()
						.Find(t)).ToListAsync(t);
					reporter.Increment();
					return allChunks;
				}

				var results = await namespaces.ParallelsAsync(loadCollChunks, 16, token);
				var jumboChunks = results.SelectMany(_ => _).ToList();
				reporter.SetCompleteMessage($"found {jumboChunks.Count} chunks.");
				return jumboChunks;
			}
		}

		private async Task<ICollection<ChunkDataSize>> scanJumboChunks(List<Chunk> jumboChunks,
			IReadOnlyDictionary<CollectionNamespace, ShardedCollectionInfo> collectionsInfo,
			CancellationToken token)
		{
			await using var reporter = _progressRenderer.Start($"Scan jumbo chunks", jumboChunks.Count);
			{
				async Task<ChunkDataSize> scanChunk(Chunk chunk, CancellationToken t)
				{
					var ns = chunk.Namespace;
					var db = _mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
					var collInfo = collectionsInfo[ns];

					var result = await db.Datasize(collInfo, chunk, true, t);

					reporter.Increment();

					if (result.IsSuccess)
						return new ChunkDataSize(ns, chunk.Shard, result.Size, result.NumObjects);

					//TODO safe error and return zero code in console
					_progressRenderer.WriteLine($"chunk {chunk.Id} - datasize command fail { result.ErrorMessage}"); //TODO write to stderr
					return null;
				}

				var results = await jumboChunks.ParallelsAsync(scanChunk, 32, token);

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
			var collectionsInfo = await _shardedCollectionService.Get(token);
			var shardedNamespaces = collectionsInfo.Values.Where(_ => !_.Dropped).Select(_ => _.Id).ToList();

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
