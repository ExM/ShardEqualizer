using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using ShardEqualizer.ByteSizeRendering;
using ShardEqualizer.ConfigServices;
using ShardEqualizer.Contracts.UI;
using ShardEqualizer.DAL;
using ShardEqualizer.DAL.Commands;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.Repositories;
using ShardEqualizer.ShardedClusterViews;

namespace ShardEqualizer.Operations
{
	public class BalancerStateOperation: IOperation
	{
		private readonly SystemDatabases _sysDbs;
		private readonly ShardsView _shardsView;
		private readonly TagRangesView _tagRangesView;
		private readonly ShardedCollectionInfoView _shardedCollectionInfoView;
		private readonly ChunkRepository _chunkRepo;
		private readonly IReadOnlyList<Interval> _intervals;
		private readonly IProgressCollector _progressRenderer;

		public BalancerStateOperation(
			SystemDatabases sysDbs,
			ShardsView shardsView,
			TagRangesView tagRangesView,
			ShardedCollectionInfoView shardedCollectionInfoView,
			ChunkRepository chunkRepo,
			IReadOnlyList<Interval> intervals,
			IProgressCollector progressRenderer)
		{
			_sysDbs = sysDbs;
			_shardsView = shardsView;
			_tagRangesView = tagRangesView;
			_shardedCollectionInfoView = shardedCollectionInfoView;
			_chunkRepo = chunkRepo;
			_intervals = intervals;
			_progressRenderer = progressRenderer;
		}

		private async Task<IList<UnMovedChunk>> scanInterval(
			Interval interval,
			IReadOnlyCollection<Shard> shards,
			IReadOnlyDictionary<CollectionNamespace, ShardedCollectionInfo> collMap,
			IReadOnlyDictionary<CollectionNamespace, IReadOnlyList<TagRange>> tagRangesByNs,
			IProgressReporter reporter,
			CancellationToken token)
		{
			var currentTags = new HashSet<TagIdentity>(interval.Zones);
			var tagRanges = tagRangesByNs[interval.Namespace];
			tagRanges = tagRanges.Where(_ => currentTags.Contains(_.Tag)).ToList();

			var result = new List<UnMovedChunk>();

			foreach (var tagRange in tagRanges)
			{
				var validShards = shards.Where(s => s.HaveTag(tagRange.Tag)).Select(s => s.Id).ToList();
				if (validShards.Count == 0)
					throw new Exception($"no shard was found containing the tag zone '{tagRange.Tag}'");

				//TODO here supports multiple shards to scan all collections in the future

				var collectionInfo = collMap[interval.Namespace];
				var unMovedChunks = await (await _chunkRepo.ByUuid(collectionInfo.Uuid)
						.From(tagRange.Min).To(tagRange.Max).NoJumbo().ExcludeShards(validShards).Find(token))
					.ToListAsync(token);

				if (unMovedChunks.Count == 0) continue;

				result.Add(new UnMovedChunk()
				{
					Namespace = interval.Namespace,
					TagRange = tagRange.Tag,
					Count = unMovedChunks.Count,
					SourceShards = unMovedChunks.Select(c => c.Shard).Distinct().Select(c => $"'{c}'").ToList(),
				});
			}

			reporter.Increment();
			return result;
		}

		private async Task<IList<UnMovedChunk>> scanIntervals(CancellationToken token)
		{
			var shards = await _shardsView.Get(token);
			var collMap = await _shardedCollectionInfoView.Get(token);
			var tagRangesByNs = await _tagRangesView.Get(_intervals.Select(i => i.Namespace), token);

			await using var reporter = _progressRenderer.Start("Scan intervals", _intervals.Count);

			var unMovedChunksList = await _intervals.ParallelsAsync((interval, t) => scanInterval(interval, shards, collMap, tagRangesByNs, reporter, t), 32, token);

			var result = unMovedChunksList.SelectMany(c => c).ToList();

			var totalUnMovedChunks = result.Sum(c => c.Count);

			reporter.SetCompleteMessage(totalUnMovedChunks == 0
				? "all chunks moved."
				: $"found {totalUnMovedChunks} chunks awaiting movement.");

			return result;
		}

		public async Task Run(CancellationToken token)
		{
			var unMovedChunks = await scanIntervals(token);

			foreach (var unMovedChunkGroup in unMovedChunks.GroupBy(c => c.Namespace).OrderBy(c => c.Key.FullName))
			{
				Console.WriteLine("{0}:", unMovedChunkGroup.Key);
				foreach (var  unMovedChunk in unMovedChunkGroup.OrderBy(c => c.TagRange))
				{
					Console.WriteLine("  tag range '{0}' waits for {1} chunks from {2} shards",
						unMovedChunk.TagRange, unMovedChunk.Count, string.Join(", ", unMovedChunk.SourceShards));
				}
			}

			var managedNs = _intervals
				.Where(i => i.Adjustable)
				.Select(i => i.Namespace)
				.ToHashSet();
			
			var shardedDataDistributions = await _sysDbs.ShardedDataDistribution(token);

			var orphanedSizeBytesByShards = shardedDataDistributions
				.Where(sdd => managedNs.Contains(sdd.Ns))
				.SelectMany(sdd => sdd.Shards)
				.GroupBy(s => s.ShardName)
				.Select(g => (key: g.Key, value: g.Sum(s => s.OrphanedSizeBytes)))
				.Where(p => p.value != 0)
				.ToDictionary(p => p.key, p => p.value);

			if (orphanedSizeBytesByShards.Any())
			{
				Console.WriteLine($"Found orphaned documents:");

				foreach (var (sh, value) in orphanedSizeBytesByShards)
				{
					Console.WriteLine($"* {sh} contains {value.ByteSize()}  bytes");
				}
			}
			else
			{
				Console.WriteLine($"Orphaned documents not found.");
			}
		}

		private class UnMovedChunk
		{
			public CollectionNamespace Namespace { get; set; }
			public TagIdentity TagRange { get; set; }
			public int Count { get; set; }
			public List<string> SourceShards { get; set; }
		}
	}
}
